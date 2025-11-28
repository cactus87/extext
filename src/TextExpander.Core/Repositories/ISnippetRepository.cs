using TextExpander.Core.Domain;

namespace TextExpander.Core.Repositories;

/// <summary>
/// 스니펫 및 카테고리 저장소 인터페이스
/// 도메인 로직과 데이터 저장 구현을 분리
/// </summary>
public interface ISnippetRepository
{
    // ========== 조회 ==========

    /// <summary>
    /// 모든 카테고리 조회
    /// </summary>
    Task<List<Category>> GetAllCategoriesAsync();

    /// <summary>
    /// 특정 카테고리의 스니펫 조회
    /// </summary>
    Task<List<Snippet>> GetSnippetsByCategoryAsync(string categoryId);

    /// <summary>
    /// 모든 활성 스니펫 조회 (isEnabled=true이고 카테고리도 활성화된 것만)
    /// </summary>
    Task<List<Snippet>> GetActiveSnippetsAsync();

    /// <summary>
    /// 키워드로 활성 스니펫 검색 (동기 메서드 - 빠른 응답 필요)
    /// </summary>
    /// <param name="keyword">검색할 키워드</param>
    /// <returns>매칭되는 스니펫 또는 null</returns>
    Snippet? GetActiveSnippetByKeyword(string keyword);

    // ========== CRUD - 카테고리 ==========

    /// <summary>
    /// 카테고리 추가
    /// </summary>
    Task<Category> AddCategoryAsync(Category category);

    /// <summary>
    /// 카테고리 수정
    /// </summary>
    Task UpdateCategoryAsync(Category category);

    /// <summary>
    /// 카테고리 삭제
    /// </summary>
    Task DeleteCategoryAsync(string categoryId);

    // ========== CRUD - 스니펫 ==========

    /// <summary>
    /// 스니펫 추가
    /// </summary>
    Task<Snippet> AddSnippetAsync(Snippet snippet);

    /// <summary>
    /// 스니펫 수정
    /// </summary>
    Task UpdateSnippetAsync(Snippet snippet);

    /// <summary>
    /// 스니펫 삭제
    /// </summary>
    Task DeleteSnippetAsync(string snippetId);

    // ========== 저장 ==========

    /// <summary>
    /// 변경 사항을 영구 저장소에 저장
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// 저장소 데이터를 파일에서 로드
    /// </summary>
    Task LoadAsync();
}

