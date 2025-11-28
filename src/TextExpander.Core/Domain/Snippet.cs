namespace TextExpander.Core.Domain;

/// <summary>
/// 텍스트 확장 스니펫 도메인 모델
/// </summary>
public class Snippet
{
    /// <summary>
    /// 스니펫 고유 식별자 (GUID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 소속 카테고리 ID (Foreign Key)
    /// </summary>
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>
    /// 트리거 키워드 (예: ";home", ";sig")
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// 대체 텍스트 (확장될 내용)
    /// </summary>
    public string Replacement { get; set; } = string.Empty;

    /// <summary>
    /// 개별 스니펫 활성화 여부
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 사용자 메모 (선택사항)
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// 생성 일시
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 최종 수정 일시
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

