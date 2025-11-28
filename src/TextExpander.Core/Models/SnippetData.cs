using TextExpander.Core.Domain;

namespace TextExpander.Core.Models;

/// <summary>
/// JSON 직렬화/역직렬화용 DTO
/// snippets.json 파일 구조를 표현
/// </summary>
public class SnippetData
{
    /// <summary>
    /// 데이터 버전 (향후 마이그레이션용)
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 최종 수정 일시
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 카테고리 목록
    /// </summary>
    public List<Category> Categories { get; set; } = new();

    /// <summary>
    /// 스니펫 목록
    /// </summary>
    public List<Snippet> Snippets { get; set; } = new();
}

