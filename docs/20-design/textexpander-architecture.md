# TextExpander 아키텍처 & 설계 (SRS - Software Requirements Specification)

## 1. 솔루션 아키텍처 개요

TextExpander는 **2계층 구조**로 설계되어 UI와 도메인 로직을 완전히 분리합니다.

```
┌─────────────────────────────────────────────────────────────┐
│                    TextExpander.App (WPF)                   │
│  ┌────────────────┐  ┌──────────────────┐  ┌──────────────┐ │
│  │  MainWindow    │  │  GlobalKeyboard  │  │  TrayIcon    │ │
│  │  + ViewModels  │  │     Hook         │  │   Service    │ │
│  └────────────────┘  └──────────────────┘  └──────────────┘ │
│                           │                                   │
│  ┌───────────────────────────────────────────────────────┐   │
│  │      KeyboardOutputService (Win32 SendInput)         │   │
│  │    - Backspace N회 입력                              │   │
│  │    - Replacement 문자열 입력                         │   │
│  │    - Delimiter 문자 입력                             │   │
│  └───────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
            ↓ (의존성: App → Core만 가능)
┌─────────────────────────────────────────────────────────────┐
│                   TextExpander.Core                          │
│  ┌────────────────┐  ┌────────────────┐  ┌──────────────┐   │
│  │  Domain        │  │  Repository    │  │   Engine     │   │
│  │  Models        │  │  (Interface &  │  │ (ExpanderEng.│   │
│  │  - Category    │  │   JSON Impl.)  │  │  + Interface)│   │
│  │  - Snippet     │  │                │  │              │   │
│  └────────────────┘  └────────────────┘  └──────────────┘   │
│                                                               │
│  Data Store: %AppData%\TextExpander\snippets.json           │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 프로젝트별 책임 정의

### 2.1 TextExpander.Core

**역할**: 도메인 모델, 비즈니스 로직, 데이터 저장소 → UI 프레임워크에 의존하지 않음

#### 2.1.1 도메인 모델

**Category.cs**
```csharp
public class Category
{
    public string Id { get; set; }              // Guid.NewGuid().ToString()
    public string Name { get; set; }            // "주소", "서명" 등
    public string Description { get; set; }    // 카테고리 설명
    public bool IsEnabled { get; set; }         // 활성화 여부
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Snippet.cs**
```csharp
public class Snippet
{
    public string Id { get; set; }
    public string CategoryId { get; set; }      // 카테고리 FK
    public string Keyword { get; set; }         // ";home", ";sig" 등
    public string Replacement { get; set; }    // 대체 텍스트
    public bool IsEnabled { get; set; }
    public string Note { get; set; }            // 메모
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### 2.1.2 저장소 (Repository Pattern)

**ISnippetRepository.cs** (인터페이스)
```csharp
public interface ISnippetRepository
{
    // 조회
    Task<List<Category>> GetAllCategoriesAsync();
    Task<List<Snippet>> GetSnippetsByCategoryAsync(string categoryId);
    Task<Snippet> GetSnippetByKeywordAsync(string keyword);
    Task<List<Snippet>> GetActiveSnippetsAsync();  // isEnabled=true인 스니펫만

    // CRUD - 카테고리
    Task<Category> AddCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);
    Task DeleteCategoryAsync(string categoryId);

    // CRUD - 스니펫
    Task<Snippet> AddSnippetAsync(Snippet snippet);
    Task UpdateSnippetAsync(Snippet snippet);
    Task DeleteSnippetAsync(string snippetId);

    // 저장
    Task SaveAsync();
}
```

**JsonSnippetRepository.cs** (구현)
- 저장 경로: `%AppData%\TextExpander\snippets.json`
- 전체 데이터를 JSON으로 로드/저장
- 메모리 캐시 + 지연 저장 (성능 최적화)
- JSON 손상 시 백업 복구 로직

#### 2.1.3 텍스트 확장 엔진

**IExpanderEngine.cs** (인터페이스)
```csharp
public interface IExpanderEngine
{
    event Action<ExpanderResult> OnExpansionNeeded;  // 치환 요청 이벤트

    void OnCharInput(char ch);
    void SetRepository(ISnippetRepository repository);
    void Enable();
    void Disable();
}
```

**ExpanderEngine.cs** (구현)
- 버퍼 관리: `StringBuilder _buffer` (최대 200자)
- 구분자 정의: 공백, 탭, 줄바꿈, 구두점
- 매칭 로직: 마지막 단어 추출 후 `ISnippetRepository`에서 검색
- 치환 신호: `OnExpansionNeeded` 이벤트 발생 (App에서 처리)

---

### 2.2 TextExpander.App (WPF)

**역할**: WPF UI, 키보드 입출력, 트레이 아이콘, Core 호출

#### 2.2.1 WPF UI

**MainWindow.xaml**
- 카테고리 리스트 (ListBox)
- 선택된 카테고리의 스니펫 리스트 (DataGrid)
- On/Off 토글 버튼
- 추가/수정/삭제 버튼
- 상태 표시 (레이블: "동작 중" / "일시 정지")

**MainWindowViewModel.cs** (MVVM Toolkit 사용)
```csharp
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CategoryViewModel> categories;

    [ObservableProperty]
    private ObservableCollection<SnippetViewModel> snippets;

    [ObservableProperty]
    private bool isEnabled;

    [RelayCommand]
    public async Task AddCategory() { ... }

    [RelayCommand]
    public async Task DeleteCategory(string categoryId) { ... }

    [RelayCommand]
    public async Task AddSnippet() { ... }

    // 기타...
}
```

#### 2.2.2 전역 키보드 후킹

**GlobalKeyboardHook.cs**
- Win32 API `SetWindowsHookEx(WH_KEYBOARD_LL, ...)`
- 저수준 키보드 이벤트 후킹 (다른 앱도 캐치 가능)
- 모든 키 입력을 `ExpanderEngine.OnCharInput(ch)`로 전달
- Backspace(`\b`), 문자, 구분자 모두 포함

#### 2.2.3 키보드 출력 서비스

**KeyboardOutputService.cs**
- Win32 `SendInput` 또는 `keybd_event` 사용
- 아래 3가지 입력을 순서대로 전송:
  1. Backspace N회 (치환 대상 단어 길이만큼)
  2. Replacement 문자열
  3. Delimiter 문자

**재귀 방지 플래그**: `_isSending` 플래그를 `GlobalKeyboardHook`에 알려서, 우리가 보낸 입력은 다시 ExpanderEngine에 진입하지 않도록 함.

```csharp
// ExpanderEngine 내부
void OnCharInput(char ch)
{
    if (_isSending) return;  // 우리가 보낸 입력 무시
    // ... 실제 처리
}

// KeyboardOutputService 사용 시
_keyboardOutputService.SetSendingFlag(true);
// SendInput 호출들...
_keyboardOutputService.SetSendingFlag(false);
```

#### 2.2.4 트레이 아이콘 서비스

**TrayIconService.cs**
- `System.Windows.Forms.NotifyIcon` 사용
- 우클릭 메뉴:
  - **열기**: `MainWindow.Show()`
  - **일시 정지**: `ExpanderEngine.Disable()`
  - **재개**: `ExpanderEngine.Enable()`
  - **종료**: 앱 종료

---

## 3. 의존성 & 흐름

### 입력 흐름 (Windows 키 입력 → ExpanderEngine → 상태 변화)

```
Windows 전역 키 입력
    ↓
GlobalKeyboardHook (Win32 후킹)
    ↓
ExpanderEngine.OnCharInput(ch)
    ├─ 버퍼 관리 (문자 추가 / Backspace 처리)
    ├─ 구분자 감지
    └─ 스니펫 매칭 (ISnippetRepository 호출)
        ↓ (매칭 성공)
    ↓
OnExpansionNeeded 이벤트 발생
    ↓
App 레이어 이벤트 핸들러 (ViewModel)
    ├─ _isSending 플래그 = true
    └─ KeyboardOutputService.Send(Backspace×N, Replacement, Delimiter)
        ↓ (Win32 SendInput)
    ↓
최종 사용자 화면에 Replacement 문자열 표시
    ↓
_isSending 플래그 = false
```

### 데이터 흐름 (UI 수정 → Core 저장소)

```
WPF UI (사용자 클릭)
    ↓
MainWindowViewModel
    ├─ ISnippetRepository.AddSnippetAsync(...)
    └─ ISnippetRepository.SaveAsync()
        ↓
    JsonSnippetRepository
        └─ %AppData%\TextExpander\snippets.json 저장
```

---

## 4. 데이터 저장 구조 (JSON 스키마)

**파일**: `%AppData%\TextExpander\snippets.json`

**상세 구조**:
```json
{
  "version": "1.0",
  "lastUpdated": "2025-11-27T03:27:00Z",
  "categories": [
    {
      "id": "97ac6c3d-3e43-4e95-9f9e-4b5a1d7e12ab",
      "name": "주소",
      "description": "자주 쓰는 주소 모음",
      "isEnabled": true,
      "createdAt": "2025-01-01T00:00:00Z",
      "updatedAt": "2025-11-27T02:00:00Z"
    }
  ],
  "snippets": [
    {
      "id": "3b0c23e5-3f1e-4d40-8e1f-bb37acbf0102",
      "categoryId": "97ac6c3d-3e43-4e95-9f9e-4b5a1d7e12ab",
      "keyword": ";home",
      "replacement": "서울특별시 강남구 테헤란로 123",
      "isEnabled": true,
      "note": "집 주소",
      "createdAt": "2025-01-01T00:00:00Z",
      "updatedAt": "2025-11-27T02:00:00Z"
    }
  ]
}
```

---

## 5. 주요 클래스 다이어그램 (의사 코드)

```
┌─────────────────────────────────────────┐
│     ISnippetRepository                  │
├─────────────────────────────────────────┤
│ + GetAllCategoriesAsync()               │
│ + GetActiveSnippetsAsync()              │
│ + AddSnippetAsync(snippet)              │
│ + SaveAsync()                           │
└────────────┬────────────────────────────┘
             △
             │
             │ (implements)
             │
┌────────────┴────────────────────────────┐
│  JsonSnippetRepository                  │
├─────────────────────────────────────────┤
│ - _cache: Dictionary<string, Category>  │
│ - _filePath: string                     │
├─────────────────────────────────────────┤
│ + LoadAsync()                           │
│ + SaveAsync()                           │
└─────────────────────────────────────────┘


┌─────────────────────────────────────────┐
│     IExpanderEngine                     │
├─────────────────────────────────────────┤
│ + OnCharInput(ch: char)                 │
│ + SetRepository(repo)                   │
│ + Enable()                              │
│ + Disable()                             │
│ event OnExpansionNeeded                 │
└────────────┬────────────────────────────┘
             △
             │ (implements)
             │
┌────────────┴────────────────────────────┐
│  ExpanderEngine                         │
├─────────────────────────────────────────┤
│ - _buffer: StringBuilder                │
│ - _repository: ISnippetRepository       │
│ - _isSending: bool                      │
│ - _isEnabled: bool                      │
├─────────────────────────────────────────┤
│ - GetLastWord(): string                 │
│ - IsDelimiter(ch): bool                 │
│ - TrimBufferIfNeeded()                  │
└─────────────────────────────────────────┘


┌─────────────────────────────────────────┐
│  GlobalKeyboardHook                     │
├─────────────────────────────────────────┤
│ - _hookHandle: IntPtr                   │
│ event KeyInputReceived(char)            │
├─────────────────────────────────────────┤
│ + Install()                             │
│ + Uninstall()                           │
│ - LowLevelKeyboardProc(...)             │
└─────────────────────────────────────────┘


┌─────────────────────────────────────────┐
│  KeyboardOutputService                  │
├─────────────────────────────────────────┤
│ - _sendingFlag: bool                    │
├─────────────────────────────────────────┤
│ + SendBackspaces(count)                 │
│ + SendText(text)                        │
│ + SendChar(ch)                          │
│ + SetSendingFlag(bool)                  │
└─────────────────────────────────────────┘
```

---

## 6. 초기화 & DI (의존성 주입) 플로우

**App.xaml.cs (WPF 진입점)**
```csharp
public partial class App : Application
{
    private ISnippetRepository _snippetRepository;
    private IExpanderEngine _expanderEngine;
    private GlobalKeyboardHook _keyboardHook;
    private KeyboardOutputService _keyboardOutputService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Core 초기화
        _snippetRepository = new JsonSnippetRepository();
        await _snippetRepository.LoadAsync();

        _expanderEngine = new ExpanderEngine();
        _expanderEngine.SetRepository(_snippetRepository);

        // 2. App 초기화
        _keyboardOutputService = new KeyboardOutputService();
        
        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.KeyInputReceived += (ch) => 
        {
            _keyboardOutputService.SetSendingFlag(ch);  // 플래그 체크
            _expanderEngine.OnCharInput(ch);
        };

        // 3. ExpanderEngine 이벤트 구독
        if (_expanderEngine is ExpanderEngine engine)
        {
            engine.OnExpansionNeeded += (result) =>
            {
                // 치환 실행
                _keyboardOutputService.SetSendingFlag(true);
                _keyboardOutputService.SendBackspaces(result.KeywordLength);
                _keyboardOutputService.SendText(result.Replacement);
                _keyboardOutputService.SendChar(result.Delimiter);
                _keyboardOutputService.SetSendingFlag(false);
            };
        }

        // 4. 키보드 후크 설치
        _keyboardHook.Install();

        // 5. MainWindow 표시
        MainWindow = new MainWindow 
        { 
            DataContext = new MainWindowViewModel(_snippetRepository, _expanderEngine)
        };
        MainWindow.Show();
    }
}
```

---

## 7. 에러 처리 & 복구

### JSON 손상 시 복구

- 저장 시: 임시 파일에 먼저 쓰기 → 기존 파일 백업 → 임시 파일을 정상 파일로 이름 변경
- 로드 시: JSON 파싱 실패 → 백업 파일 시도 → 백업도 실패 → 기본 빈 상태로 시작

### 키보드 후크 실패

- 후크 설치 실패 시: 사용자에게 경고 → 관리자 권한 재실행 제시
- 후크 제거 실패 시: 종료 시 경고 로그 출력

### 치환 실패

- 치환 중 예외 발생 시: 로그 기록 → 버퍼 롤백 → 구분자만 입력

---

## 8. 확장성 고려 사항

### 플레이스홀더 지원 예시

**향후 확장 시**:
- `{date}` → 현재 날짜
- `{time}` → 현재 시간
- `{clipboard}` → 클립보드 내용

**구현 방식**: `IReplacementExpander` 인터페이스 추가
```csharp
public interface IReplacementExpander
{
    string ExpandPlaceholders(string replacement);
}

// ExpanderEngine에서 사용
var expandedReplacement = _replacementExpander.ExpandPlaceholders(snippet.Replacement);
```

### 예외 앱 리스트

**향후 추가**:
- 특정 앱(예: 암호 관리자)에서는 확장하지 않도록 설정
- `GlobalKeyboardHook`에서 활성 윈도우 타이틀 체크 → 제외 앱이면 `OnCharInput` 호출 안 함

---

## 9. 배포 & 설치

### MVP 배포 방식

- 단일 EXE: `.NET 8 Runtime` 필수 → 사용자가 별도 설치 필요
- 자체 포함 EXE: `RuntimeIdentifier = win-x64` 옵션으로 EXE에 런타임 번들 → 배포 파일 크기 증가

### 권장 (초기): ZIP 배포 또는 단순 설치 프로그램 (WiX Toolset)

---

## 10. 성능 & 모니터링

### 메모리 프로파일링

- 로딩 후 메모리: ~50MB
- 장시간 운영 시: 100MB 이하 유지 목표

### CPU 사용량

- 유휴 상태: 거의 0%
- 활성 타이핑: < 1% (키보드 이벤트 처리만)

### 응답 시간

- 구분자 입력 → 치환 완료: < 50ms

### 로깅 & 디버깅

- 로그 파일: `%AppData%\TextExpander\logs\`
- 로그 레벨: Info, Warning, Error
- 스니펫 매칭 실패 등은 Warning 레벨로 기록
