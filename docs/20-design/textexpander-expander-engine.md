# TextExpander ExpanderEngine 핵심 설계 (상세 명세)

## 1. 목적 & 책임

`ExpanderEngine`은 TextExpander의 **핵심 도메인 엔진**으로, 다음을 담당합니다:

- 전역 키보드 후킹에서 받은 사용자 입력(단일 문자) 스트림을 처리
- 최근 입력 버퍼를 관리해 "마지막 단어" 추출 가능하게 유지
- 사용자가 구분자를 입력하면 마지막 단어가 활성 스니펫의 키워드와 정확히 일치하는지 검색
- 매칭 성공 시 "해당 단어를 대체 텍스트로 치환해야 한다"는 신호를 외부(`IKeyboardOutput` / 이벤트)로 전달
- **실제 키 이벤트 송신(Backspace, Replacement 문자, Delimiter)은 App 레이어의 `KeyboardOutputService`가 담당** (관심사의 분리)

---

## 2. 구분자(Delimiter) 정의

구분자는 "**단어의 끝**"을 의미하는 문자들입니다. 구분자가 입력되면 버퍼에 있는 마지막 단어를 스니펫 매칭 대상으로 삼습니다.

### 기본 구분자 목록

| 문자 | Unicode | 설명 | 사용 예 |
|------|---------|------|--------|
| ` ` (공백) | U+0020 | 단어 간 구분 | `;home ` → "서울..." 치환 |
| `\t` (탭) | U+0009 | 들여쓰기/칼럼 이동 | `;sig\t` → "Best regards..." 치환 |
| `\r` (CR) | U+000D | 캐리지 리턴 (Windows 줄바꿈 일부) | (일반적으로 안 봄) |
| `\n` (LF) | U+000A | 라인 피드 (줄바꿈) | `;addr\n` → 주소 치환 후 줄바꿈 |
| `.` (마침표) | U+002E | 문장 끝 | "This is ;sig.Hello" |
| `,` (쉼표) | U+002C | 리스트 구분 | ";email, please" |
| `!` (느낌표) | U+0021 | 강조 | "Hello ;name!" |
| `?` (물음표) | U+003F | 질문 | "Who is ;name?" |
| `:` (콜론) | U+003A | 라벨/비율 | "Name: ;name" |
| `;` (세미콜론) | U+003B | 문장 구분 | "First;second; third;" |

### 구분자가 아닌 문자

다음은 단어의 일부로 간주되어 버퍼에 축적됩니다:
- 영문자: `a-z`, `A-Z`
- 숫자: `0-9`
- 특수문자 (일부): `-` (하이픈), `_` (언더스코어), `@` (이메일용)

**예시**:
- `;email-work` → 키워드 그대로 축적
- `;email@work` → 키워드 그대로 축적

---

## 3. 버퍼 관리 정책

### 3.1 버퍼의 역할

```
버퍼 = 사용자의 최근 타이핑 기록
(전체 문맥이 아니라, "재모양" 추적용 임시 저장소)
```

내부적으로 `StringBuilder _buffer`를 사용하여 입력된 문자들을 순서대로 쌓습니다.

### 3.2 버퍼 크기 제한

```csharp
private const int MAX_BUFFER_LENGTH = 200;  // 최대 200자

// 버퍼 크기 초과 시 앞부분 잘라내기
if (_buffer.Length > MAX_BUFFER_LENGTH)
{
    _buffer.Remove(0, _buffer.Length - MAX_BUFFER_LENGTH);
}
```

**이유**:
- 메모리 사용량 제어 (백그라운드 상시 동작)
- 장시간 운영 시 메모리 누수 방지
- 일반적인 키워드 길이가 수십 자 이하이므로 200자면 충분

### 3.3 Backspace 처리

```csharp
if (ch == '\b')  // Backspace 문자
{
    if (_buffer.Length > 0)
    {
        _buffer.Remove(_buffer.Length - 1, 1);  // 마지막 문자 제거
    }
    // Backspace는 구분자가 아니므로 여기서 반환
    return;
}
```

**동작**:
- 사용자가 Backspace를 입력하면 버퍼의 마지막 문자를 제거
- 버퍼가 비어 있으면 아무것도 하지 않음

---

## 4. 처리 흐름 (상세 의사코드)

```csharp
public class ExpanderEngine : IExpanderEngine
{
    private StringBuilder _buffer = new StringBuilder();
    private ISnippetRepository _repository;
    private bool _isSending = false;  // 우리가 보낸 입력 여부
    private bool _isEnabled = true;   // 엔진 활성화 여부

    // 외부에서 구독할 이벤트
    public event Action<ExpanderResult> OnExpansionNeeded;

    /// <summary>
    /// 글로벌 키보드 후크에서 호출됨 (모든 키 입력)
    /// </summary>
    public void OnCharInput(char ch)
    {
        // 1. 기본 검사
        if (!_isEnabled)
            return;

        if (_isSending)
            return;  // 우리가 보낸 입력은 무시

        // 2. Backspace 처리
        if (ch == '\b')
        {
            HandleBackspace();
            return;  // Backspace는 이것으로 끝
        }

        // 3. 구분자 검사
        if (IsDelimiter(ch))
        {
            HandleDelimiter(ch);
        }
        else
        {
            // 4. 일반 문자 → 버퍼에 추가
            _buffer.Append(ch);
        }

        // 5. 버퍼 크기 관리
        TrimBufferIfNeeded();
    }

    private void HandleBackspace()
    {
        if (_buffer.Length > 0)
        {
            _buffer.Remove(_buffer.Length - 1, 1);
        }
    }

    private void HandleDelimiter(char delimiter)
    {
        // 1. 버퍼에서 마지막 단어 추출
        string lastWord = ExtractLastWord();

        // 2. 활성 스니펫 검색
        Snippet matchedSnippet = _repository.GetActiveSnippetByKeyword(lastWord);

        if (matchedSnippet != null)
        {
            // 3. 매칭 성공 → 치환 이벤트 발생
            var result = new ExpanderResult
            {
                Keyword = lastWord,
                Replacement = matchedSnippet.Replacement,
                Delimiter = delimiter,
                KeywordLength = lastWord.Length
            };

            OnExpansionNeeded?.Invoke(result);

            // 4. 버퍼를 리셋하지 않음
            // 왜? App이 Backspace(×N) + Replacement + Delimiter를 보낸 후,
            // 이 입력들이 다시 OnCharInput에 들어올 때
            // _isSending=true 상태이므로 버퍼가 갱신되지 않기 때문.
            // 따라서 버퍼는 그냥 두면 됨.
        }
        else
        {
            // 5. 매칭 실패 → 구분자만 버퍼에 추가
            _buffer.Append(delimiter);
        }
    }

    private string ExtractLastWord()
    {
        // 버퍼의 끝에서부터 역순으로 탐색하여
        // 첫 번째 "구분자 패턴"을 만나기 전까지의 문자를 추출
        
        string bufferStr = _buffer.ToString();
        
        // 끝부터 역순으로 구분자 찾기
        int lastDelimiterIndex = -1;
        for (int i = bufferStr.Length - 1; i >= 0; i--)
        {
            if (IsDelimiter(bufferStr[i]) || IsWhitespace(bufferStr[i]))
            {
                lastDelimiterIndex = i;
                break;
            }
        }

        // 마지막 단어 추출
        int startIndex = lastDelimiterIndex + 1;
        string lastWord = bufferStr.Substring(startIndex);

        return lastWord;
    }

    private bool IsDelimiter(char ch)
    {
        return ch switch
        {
            ' ' or '\t' or '\r' or '\n' => true,  // 공백
            '.' or ',' or '!' or '?' or ':' or ';' => true,  // 구두점
            _ => false
        };
    }

    private bool IsWhitespace(char ch)
    {
        return char.IsWhiteSpace(ch);
    }

    private void TrimBufferIfNeeded()
    {
        if (_buffer.Length > MAX_BUFFER_LENGTH)
        {
            int excessLength = _buffer.Length - MAX_BUFFER_LENGTH;
            _buffer.Remove(0, excessLength);
        }
    }

    public void Enable()
    {
        _isEnabled = true;
    }

    public void Disable()
    {
        _isEnabled = false;
    }

    public void SetSendingFlag(bool sending)
    {
        _isSending = sending;
    }

    public void SetRepository(ISnippetRepository repository)
    {
        _repository = repository;
    }
}
```

---

## 5. 핵심 알고리즘 설명

### 5.1 "마지막 단어" 추출 알고리즘

**예시 시나리오**:
```
버퍼: "Hello ;home world ;addr test"
구분자 입력: [스페이스]

마지막 단어 추출:
1. 버퍼 문자열 끝에서 역순 탐색
2. "test" 뒤에 공백 발견 → 시작 인덱스 확인
3. "test" 반환
```

**코드**:
```csharp
string bufferStr = "Hello ;home world ;addr test";
// 끝부터 역순 탐색 → 공백 위치 찾음
int lastDelimiterPos = bufferStr.LastIndexOfAny(new[] { ' ', '\t', '\n', '\r' });
string lastWord = bufferStr.Substring(lastDelimiterPos + 1);
// 결과: lastWord = "test"
```

### 5.2 재귀 방지 플래그 (`_isSending`)

**흐름**:

```
사용자 입력: ";home " (구분자는 공백)
    ↓
OnCharInput(';'), OnCharInput('h'), ... OnCharInput(' ')
    ↓
HandleDelimiter(' ') 감지
    ↓
";home" 매칭 → "서울..."로 치환 필요
    ↓
OnExpansionNeeded 이벤트 발생
    ↓
App의 KeyboardOutputService가 처리:
  _isSending = true
  SendInput(Backspace×5)  // ';', 'h', 'o', 'm', 'e' 지우기
  SendInput("서울특별시 강남구...")  // 대체 텍스트 입력
  SendInput(' ')  // 구분자 다시 입력
  _isSending = false
    ↓
ExpanderEngine의 OnCharInput이 다시 호출되지만,
_isSending=true이므로 즉시 반환 (처리 안 함)
```

**이유**: 우리가 보낸 입력(Backspace, Replacement, Delimiter)이 다시 ExpanderEngine에 들어오는 것을 방지

---

## 6. 스니펫 저장소 인터페이스

ExpanderEngine은 `ISnippetRepository`를 의존하여, 스니펫 검색 시 다음을 호출합니다:

```csharp
public interface ISnippetRepository
{
    /// <summary>
    /// 활성 상태(isEnabled=true)인 스니펫 중
    /// 키워드가 정확히 일치하는 것을 반환
    /// </summary>
    Snippet GetActiveSnippetByKeyword(string keyword);

    // 기타 메서드들...
}
```

**구현 책임** (JsonSnippetRepository):
- 모든 활성 카테고리의 활성 스니펫만 조회
- 키워드 정확 일치 (대소문자 구분 또는 무시 설정 가능)
- 캐시를 활용해 성능 최적화

---

## 7. 이벤트 & 콜백

### 7.1 OnExpansionNeeded 이벤트

```csharp
public class ExpanderResult
{
    public string Keyword { get; set; }           // ";home"
    public string Replacement { get; set; }       // "서울특별시..."
    public char Delimiter { get; set; }           // ' ' (공백)
    public int KeywordLength { get; set; }        // 5
}

public event Action<ExpanderResult> OnExpansionNeeded;
```

**사용처** (App 레이어):
```csharp
expanderEngine.OnExpansionNeeded += (result) =>
{
    keyboardOutputService.SetSendingFlag(true);
    keyboardOutputService.SendBackspaces(result.KeywordLength);
    keyboardOutputService.SendText(result.Replacement);
    keyboardOutputService.SendChar(result.Delimiter);
    keyboardOutputService.SetSendingFlag(false);
};
```

---

## 8. 엣지 케이스 & 예외 처리

### 8.1 빈 버퍼 상태에서 구분자 입력

```csharp
// 버퍼: "" (비어있음)
// 입력: [스페이스]

ExtractLastWord() → ""  // 빈 문자열 반환
GetActiveSnippetByKeyword("") → null  // 매칭 없음
_buffer.Append(' ') → 버퍼: " "
```

**결과**: 구분자만 버퍼에 추가됨 (정상 동작)

### 8.2 연속된 구분자

```csharp
// 버퍼: " "
// 입력: [스페이스]

ExtractLastWord() → ""
GetActiveSnippetByKeyword("") → null
_buffer.Append(' ') → 버퍼: "  "
```

**결과**: 공백들이 쌓임 (정상)

### 8.3 Backspace로 전체 삭제

```csharp
// 버퍼: ";home"
// 연속 Backspace 5회

결과: 버퍼 = "" (비어있음)
```

**결과**: 정상 (다음 구분자 입력 시 빈 문자열 매칭)

### 8.4 매우 긴 키워드 입력

```csharp
// 버퍼: "aaa...aaa" (199자)
// 입력: "b" (200자 초과)

TrimBufferIfNeeded() 호출 → 앞 1자 제거
버퍼: "aa...aaab" (200자 정확히)
```

**결과**: 메모리 안전하게 유지

---

## 9. 성능 고려사항

### 9.1 복잡도 분석

| 연산 | 복잡도 | 설명 |
|------|--------|------|
| `OnCharInput()` | O(1) | Backspace 처리, 구분자 검사, 버퍼 추가 |
| `ExtractLastWord()` | O(n) | n = 버퍼 길이 (최대 200) → 실질적으로 O(1) |
| `GetActiveSnippetByKeyword()` | O(m) | m = 활성 스니펫 수 (일반적으로 수백~수천) |
| 전체 평균 | < 1ms | 키 입력 당 처리 시간 |

### 9.2 메모리 사용

- 버퍼: 최대 200자 × 2바이트(UTF-16) ≈ 400 bytes
- 이벤트 구독자: 몇 개 포인터 + 델리게이트 ≈ 수 KB
- **전체**: < 1MB (거의 무시할 수준)

---

## 10. 테스트 케이스 (Unit Test)

```csharp
[TestClass]
public class ExpanderEngineTests
{
    private ExpanderEngine _engine;
    private Mock<ISnippetRepository> _mockRepo;

    [TestInitialize]
    public void Setup()
    {
        _engine = new ExpanderEngine();
        _mockRepo = new Mock<ISnippetRepository>();
        _engine.SetRepository(_mockRepo.Object);
    }

    [TestMethod]
    public void OnCharInput_WithDelimiter_ShouldTriggerExpansion()
    {
        // Arrange
        var snippet = new Snippet { Keyword = ";home", Replacement = "서울..." };
        _mockRepo.Setup(r => r.GetActiveSnippetByKeyword(";home"))
            .Returns(snippet);

        bool eventTriggered = false;
        _engine.OnExpansionNeeded += (result) => eventTriggered = true;

        // Act
        _engine.OnCharInput(';');
        _engine.OnCharInput('h');
        _engine.OnCharInput('o');
        _engine.OnCharInput('m');
        _engine.OnCharInput('e');
        _engine.OnCharInput(' ');  // 구분자

        // Assert
        Assert.IsTrue(eventTriggered);
        _mockRepo.Verify(r => r.GetActiveSnippetByKeyword(";home"), Times.Once);
    }

    [TestMethod]
    public void OnCharInput_WithBackspace_ShouldRemoveLastChar()
    {
        // Arrange
        _engine.OnCharInput('t');
        _engine.OnCharInput('e');
        _engine.OnCharInput('s');
        _engine.OnCharInput('t');

        // Act
        _engine.OnCharInput('\b');  // Backspace

        // Assert
        // 내부 상태 검증 (리플렉션 또는 public 메서드)
    }
}
```

---

## 11. 관련 파일

- **인터페이스 정의**: `TextExpander.Core/Engine/IExpanderEngine.cs`
- **구현**: `TextExpander.Core/Engine/ExpanderEngine.cs`
- **저장소 인터페이스**: `TextExpander.Core/Repositories/ISnippetRepository.cs`
- **App 이벤트 핸들러**: `TextExpander.App/Services/KeyboardOutputService.cs`
