namespace TextExpander.Core.Models;

/// <summary>
/// 텍스트 확장 요청 결과를 담는 DTO
/// ExpanderEngine이 OnExpansionNeeded 이벤트 발생 시 전달
/// </summary>
public class ExpanderResult
{
    /// <summary>
    /// 매칭된 키워드 (예: ";home")
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// 대체할 텍스트
    /// </summary>
    public string Replacement { get; set; } = string.Empty;

    /// <summary>
    /// 트리거된 구분자 문자 (예: ' ', '.', '\n')
    /// </summary>
    public char Delimiter { get; set; }

    /// <summary>
    /// 키워드 길이 (Backspace 횟수 계산용)
    /// </summary>
    public int KeywordLength { get; set; }
}

