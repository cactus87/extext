using TextExpander.Core.Domain;
using TextExpander.Core.Repositories;

namespace TextExpander.Core.Tests;

/// <summary>
/// JsonSnippetRepository 단위 테스트
/// </summary>
public class JsonSnippetRepositoryTests
{
    private readonly string _testAppDataPath;

    public JsonSnippetRepositoryTests()
    {
        // 테스트용 임시 디렉터리 사용
        _testAppDataPath = Path.Combine(Path.GetTempPath(), "TextExpanderTests", Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task AddCategoryAsync_ShouldAddCategory()
    {
        // Arrange
        var repository = new JsonSnippetRepository();

        // Act
        var category = await repository.AddCategoryAsync(new Category
        {
            Name = "테스트 카테고리",
            Description = "설명"
        });

        // Assert
        Assert.NotNull(category.Id);
        Assert.NotEmpty(category.Id);
        var categories = await repository.GetAllCategoriesAsync();
        Assert.Single(categories);
    }

    [Fact]
    public async Task AddSnippetAsync_ShouldAddSnippet()
    {
        // Arrange
        var repository = new JsonSnippetRepository();
        var category = await repository.AddCategoryAsync(new Category
        {
            Name = "카테고리",
            IsEnabled = true
        });

        // Act
        var snippet = await repository.AddSnippetAsync(new Snippet
        {
            CategoryId = category.Id,
            Keyword = ";test",
            Replacement = "테스트 내용"
        });

        // Assert
        Assert.NotNull(snippet.Id);
        Assert.NotEmpty(snippet.Id);
        var snippets = await repository.GetSnippetsByCategoryAsync(category.Id);
        Assert.Single(snippets);
    }

    [Fact]
    public async Task GetActiveSnippetsAsync_ShouldReturnOnlyActiveSnippets()
    {
        // Arrange
        var repository = new JsonSnippetRepository();
        
        var activeCategory = await repository.AddCategoryAsync(new Category
        {
            Name = "활성 카테고리",
            IsEnabled = true
        });
        
        var inactiveCategory = await repository.AddCategoryAsync(new Category
        {
            Name = "비활성 카테고리",
            IsEnabled = false
        });

        await repository.AddSnippetAsync(new Snippet
        {
            CategoryId = activeCategory.Id,
            Keyword = ";active",
            Replacement = "활성",
            IsEnabled = true
        });

        await repository.AddSnippetAsync(new Snippet
        {
            CategoryId = activeCategory.Id,
            Keyword = ";inactive",
            Replacement = "비활성",
            IsEnabled = false
        });

        await repository.AddSnippetAsync(new Snippet
        {
            CategoryId = inactiveCategory.Id,
            Keyword = ";catinactive",
            Replacement = "카테고리 비활성",
            IsEnabled = true
        });

        // Act
        var activeSnippets = await repository.GetActiveSnippetsAsync();

        // Assert
        Assert.Single(activeSnippets);
        Assert.Equal(";active", activeSnippets[0].Keyword);
    }

    [Fact]
    public async Task GetActiveSnippetByKeyword_ShouldReturnMatchingSnippet()
    {
        // Arrange
        var repository = new JsonSnippetRepository();
        
        var category = await repository.AddCategoryAsync(new Category
        {
            Name = "카테고리",
            IsEnabled = true
        });

        await repository.AddSnippetAsync(new Snippet
        {
            CategoryId = category.Id,
            Keyword = ";home",
            Replacement = "주소",
            IsEnabled = true
        });

        // Act
        var snippet = repository.GetActiveSnippetByKeyword(";home");

        // Assert
        Assert.NotNull(snippet);
        Assert.Equal("주소", snippet.Replacement);
    }

    [Fact]
    public async Task GetActiveSnippetByKeyword_WithDisabledCategory_ShouldReturnNull()
    {
        // Arrange
        var repository = new JsonSnippetRepository();
        
        var category = await repository.AddCategoryAsync(new Category
        {
            Name = "카테고리",
            IsEnabled = false // 비활성
        });

        await repository.AddSnippetAsync(new Snippet
        {
            CategoryId = category.Id,
            Keyword = ";test",
            Replacement = "테스트",
            IsEnabled = true
        });

        // Act
        var snippet = repository.GetActiveSnippetByKeyword(";test");

        // Assert
        Assert.Null(snippet);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ShouldUpdateCategory()
    {
        // Arrange
        var repository = new JsonSnippetRepository();
        var category = await repository.AddCategoryAsync(new Category
        {
            Name = "원본 이름",
            Description = "원본 설명"
        });

        // Act
        category.Name = "수정된 이름";
        category.Description = "수정된 설명";
        await repository.UpdateCategoryAsync(category);

        // Assert
        var categories = await repository.GetAllCategoriesAsync();
        var updated = categories.First(c => c.Id == category.Id);
        Assert.Equal("수정된 이름", updated.Name);
        Assert.Equal("수정된 설명", updated.Description);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldRemoveCategoryAndSnippets()
    {
        // Arrange
        var repository = new JsonSnippetRepository();
        var category = await repository.AddCategoryAsync(new Category { Name = "삭제할 카테고리" });
        await repository.AddSnippetAsync(new Snippet
        {
            CategoryId = category.Id,
            Keyword = ";del",
            Replacement = "삭제될 스니펫"
        });

        // Act
        await repository.DeleteCategoryAsync(category.Id);

        // Assert
        var categories = await repository.GetAllCategoriesAsync();
        Assert.Empty(categories);
        
        var snippets = await repository.GetSnippetsByCategoryAsync(category.Id);
        Assert.Empty(snippets);
    }

    [Fact]
    public async Task SaveAndLoadAsync_ShouldPersistData()
    {
        // Arrange - 기존 데이터 삭제 후 시작
        var repository1 = new JsonSnippetRepository();
        await repository1.LoadAsync(); // 기존 데이터 로드
        
        var category = await repository1.AddCategoryAsync(new Category
        {
            Name = "저장 테스트",
            IsEnabled = true
        });
        
        await repository1.AddSnippetAsync(new Snippet
        {
            CategoryId = category.Id,
            Keyword = ";savetest",
            Replacement = "저장된 내용",
            IsEnabled = true
        });

        // Act
        await repository1.SaveAsync();

        // 새 인스턴스로 로드
        var repository2 = new JsonSnippetRepository();
        await repository2.LoadAsync();

        // Assert
        var snippet = repository2.GetActiveSnippetByKeyword(";savetest");
        Assert.NotNull(snippet);
        Assert.Equal("저장된 내용", snippet.Replacement);
    }

    [Fact]
    public async Task LoadAsync_InitialState_ShouldWork()
    {
        // Arrange
        var repository = new JsonSnippetRepository();

        // Act
        await repository.LoadAsync(); // 파일이 있든 없든 로드

        // Assert - 로드 후 정상 작동 확인
        var categories = await repository.GetAllCategoriesAsync();
        Assert.NotNull(categories); // null이 아닌 리스트 반환
    }
}

