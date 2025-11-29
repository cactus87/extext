using System.Text;
using TextExpander.Core.Models;
using TextExpander.Core.Repositories;

namespace TextExpander.Core.Engine;

/// <summary>
/// 텍스트 확장 엔진 구현
/// 입력 버퍼를 관리하고 구분자 감지 시 스니펫 매칭 수행
/// </summary>
public class ExpanderEngine : IExpanderEngine
{
    private const int MAX_BUFFER_LENGTH = 200;
    
    private readonly StringBuilder _buffer = new();
    private ISnippetRepository? _repository;
    private AppSettings _settings = new();
    private bool _isSending = false;
    private bool _isEnabled = true;
    private DateTime _lastProcessTime = DateTime.MinValue;

    /// <inheritdoc/>
    public event Action<ExpanderResult>? OnExpansionNeeded;

    /// <inheritdoc/>
    public void OnCharInput(char ch)
    {
        // 1. 기본 검사
        if (!_isEnabled || _isSending)
            return;

        // 2. 폴링 간격 체크 (저사양 PC를 위한 처리 간격 조절)
        if (_settings.KeyProcessingIntervalMs > 0)
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastProcessTime).TotalMilliseconds;
            if (elapsed < _settings.KeyProcessingIntervalMs)
            {
                return; // 너무 빠른 입력은 건너뜀
            }
            _lastProcessTime = now;
        }

        // 3. Backspace 처리: 버퍼에서 마지막 문자 1개 제거 (사용자 실수 수정 허용)
        if (ch == '\b')
        {
            if (_buffer.Length > 0)
            {
                _buffer.Remove(_buffer.Length - 1, 1);
            }
            return;
        }

        // 4. Enter, Space 처리: 버퍼 전체 리셋
        if (ch == '\n' || ch == ' ')
        {
            _buffer.Clear();
            return;
        }

        // 5. 구분자 검사
        if (IsDelimiter(ch))
        {
            HandleDelimiter(ch);
        }
        else
        {
            // 6. 일반 문자 → 버퍼에 추가
            _buffer.Append(ch);
        }

        // 7. 버퍼 크기 관리
        TrimBufferIfNeeded();
    }

    /// <inheritdoc/>
    public void SetRepository(ISnippetRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc/>
    public void SetSettings(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc/>
    public void Enable()
    {
        _isEnabled = true;
    }

    /// <inheritdoc/>
    public void Disable()
    {
        _isEnabled = false;
    }

    /// <inheritdoc/>
    public void SetSendingFlag(bool sending)
    {
        _isSending = sending;
    }

    /// <inheritdoc/>
    public void ResetBuffer()
    {
        _buffer.Clear();
    }

    /// <summary>
    /// 구분자 입력 처리: 마지막 단어 추출 및 스니펫 매칭
    /// </summary>
    private void HandleDelimiter(char delimiter)
    {
        // 1. 버퍼에서 마지막 단어 추출
        string lastWord = ExtractLastWord();

        // 2. 빈 단어는 무시하고 버퍼 클리어
        if (string.IsNullOrWhiteSpace(lastWord))
        {
            _buffer.Clear();
            return;
        }

        // 3. 저장소가 없으면 매칭 불가, 버퍼 클리어
        if (_repository == null)
        {
            _buffer.Clear();
            return;
        }

        // 4. 활성 스니펫 검색
        var matchedSnippet = _repository.GetActiveSnippetByKeyword(lastWord);

        // 5. 매칭 성공 시 치환 이벤트 발생
        if (matchedSnippet != null)
        {
            var result = new ExpanderResult
            {
                Keyword = lastWord,
                Replacement = matchedSnippet.Replacement,
                Delimiter = delimiter,
                KeywordLength = lastWord.Length
            };

            OnExpansionNeeded?.Invoke(result);
        }

        // 6. 매칭 성공/실패 관계없이 버퍼 클리어 (일관성 유지)
        _buffer.Clear();
    }

    /// <summary>
    /// 버퍼에서 마지막 단어 추출
    /// 구분자를 만나기 전까지의 문자열 반환
    /// </summary>
    private string ExtractLastWord()
    {
        string bufferStr = _buffer.ToString();
        
        // 끝에서부터 역순으로 구분자/공백 찾기
        for (int i = bufferStr.Length - 1; i >= 0; i--)
        {
            if (IsDelimiter(bufferStr[i]) || char.IsWhiteSpace(bufferStr[i]))
            {
                // 구분자 이후의 문자열 반환
                return bufferStr.Substring(i + 1);
            }
        }

        // 구분자가 없으면 전체 버퍼가 마지막 단어
        return bufferStr;
    }

    /// <summary>
    /// 문자가 구분자인지 판별
    /// 설정에 따라 동적으로 구분자 결정
    /// </summary>
    private bool IsDelimiter(char ch)
    {
        return ch switch
        {
            '\t' => _settings.UseTabAsDelimiter,
            '.' => _settings.UsePeriodAsDelimiter,
            ',' => _settings.UseCommaAsDelimiter,
            ';' => _settings.UseSemicolonAsDelimiter,
            '`' => _settings.UseBacktickAsDelimiter,
            '\'' => _settings.UseSingleQuoteAsDelimiter,
            '/' => _settings.UseSlashAsDelimiter,
            _ => false
        };
    }

    /// <summary>
    /// 버퍼가 최대 크기를 초과하면 앞부분 제거
    /// </summary>
    private void TrimBufferIfNeeded()
    {
        if (_buffer.Length > MAX_BUFFER_LENGTH)
        {
            int excessLength = _buffer.Length - MAX_BUFFER_LENGTH;
            _buffer.Remove(0, excessLength);
        }
    }
}

