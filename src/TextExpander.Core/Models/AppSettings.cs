namespace TextExpander.Core.Models;

/// <summary>
/// 애플리케이션 설정 모델
/// </summary>
public class AppSettings
{
    private int _keyProcessingIntervalMs = 0;
    private int _backspaceDelayMs = 10;
    private int _textCharDelayMs = 5;

    // 구분자 키 설정
    public bool UseTabAsDelimiter { get; set; } = true;
    public bool UsePeriodAsDelimiter { get; set; } = true;
    public bool UseCommaAsDelimiter { get; set; } = true;
    public bool UseSemicolonAsDelimiter { get; set; } = true;
    public bool UseBacktickAsDelimiter { get; set; } = true;
    public bool UseSingleQuoteAsDelimiter { get; set; } = true;
    public bool UseSlashAsDelimiter { get; set; } = true;
    
    // 효과음
    public bool PlaySoundOnExpansion { get; set; } = false;
    
    // 키 입력 처리 간격 (ms) - 저사양 PC를 위한 폴링 주기 조절
    public int KeyProcessingIntervalMs 
    { 
        get => _keyProcessingIntervalMs; 
        set => _keyProcessingIntervalMs = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "값은 0 이상이어야 합니다.");
    }
    
    // 키보드 출력 딜레이 (ms)
    public int BackspaceDelayMs 
    { 
        get => _backspaceDelayMs; 
        set => _backspaceDelayMs = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "값은 0 이상이어야 합니다.");
    }
    
    public int TextCharDelayMs 
    { 
        get => _textCharDelayMs; 
        set => _textCharDelayMs = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "값은 0 이상이어야 합니다.");
    }
}
