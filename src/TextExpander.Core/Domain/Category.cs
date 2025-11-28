namespace TextExpander.Core.Domain;

/// <summary>
/// 스니펫을 그룹화하는 카테고리 도메인 모델
/// </summary>
public class Category
{
    /// <summary>
    /// 카테고리 고유 식별자 (GUID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 카테고리 이름 (예: "주소", "서명")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 카테고리 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 활성화 여부. false이면 이 카테고리의 모든 스니펫이 비활성화된 것으로 간주
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 배경 색상 (Hex 코드, 예: #E0E0E0)
    /// </summary>
    public string BackgroundColor { get; set; } = "#E0E0E0";

    /// <summary>
    /// 글자 색상 (Hex 코드, 예: #000000)
    /// </summary>
    public string ForegroundColor { get; set; } = "#000000";

    /// <summary>
    /// 생성 일시
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 최종 수정 일시
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

