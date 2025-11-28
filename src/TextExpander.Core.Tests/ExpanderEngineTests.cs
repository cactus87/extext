using TextExpander.Core.Domain;
using TextExpander.Core.Engine;
using TextExpander.Core.Models;
using TextExpander.Core.Repositories;
using Xunit.Abstractions;

namespace TextExpander.Core.Tests;

/// <summary>
/// ExpanderEngine 단위 테스트
/// </summary>
public class ExpanderEngineTests
{
    [Fact]
    public void OnCharInput_WithDelimiter_ShouldTriggerExpansion()
    {
        // Arrange
        var engine = new ExpanderEngine();
        var repository = new MockSnippetRepository();
        
        var category = new Category { Id = "cat1", Name = "Category", IsEnabled = true };
        repository.AddCategory(category);
        
        var snippet = new Snippet
        {
            Id = "1",
            CategoryId = "cat1",
            Keyword = ";home",
            Replacement = "서울특별시 강남구 테헤란로 123",
            IsEnabled = true
        };
        repository.AddSnippet(snippet);
        
        engine.SetRepository(repository);

        ExpanderResult? result = null;
        engine.OnExpansionNeeded += (r) => result = r;

        // Act
        foreach (char ch in ";home ")
        {
            engine.OnCharInput(ch);
        }

        // Assert
        Assert.NotNull(result);
        Assert.Equal(";home", result.Keyword);
        Assert.Equal("서울특별시 강남구 테헤란로 123", result.Replacement);
        Assert.Equal(' ', result.Delimiter);
        Assert.Equal(5, result.KeywordLength);
    }

    [Fact]
    public void OnCharInput_WithBackspace_ShouldRemoveLastChar()
    {
        // Arrange
        var engine = new ExpanderEngine();
        var repository = new MockSnippetRepository();
        engine.SetRepository(repository);

        bool eventTriggered = false;
        engine.OnExpansionNeeded += (_) => eventTriggered = true;

        // Act
        foreach (char ch in "test")
        {
            engine.OnCharInput(ch);
        }
        engine.OnCharInput('\b'); // Backspace
        engine.OnCharInput('\b'); // Backspace
        engine.OnCharInput(' ');  // 구분자

        // Assert - "te"가 키워드로 검색되어야 하지만 없으므로 이벤트 없음
        Assert.False(eventTriggered);
    }

    [Fact]
    public void OnCharInput_WhenDisabled_ShouldNotProcess()
    {
        // Arrange
        var engine = new ExpanderEngine();
        var repository = new MockSnippetRepository();
        
        var category = new Category { Id = "cat1", Name = "Category", IsEnabled = true };
        repository.AddCategory(category);
        
        repository.AddSnippet(new Snippet
        {
            Id = "1",
            CategoryId = "cat1",
            Keyword = ";sig",
            Replacement = "Best regards",
            IsEnabled = true
        });
        
        engine.SetRepository(repository);
        engine.Disable();

        bool eventTriggered = false;
        engine.OnExpansionNeeded += (_) => eventTriggered = true;

        // Act
        foreach (char ch in ";sig ")
        {
            engine.OnCharInput(ch);
        }

        // Assert
        Assert.False(eventTriggered);
    }

    [Fact]
    public void OnCharInput_WhenSending_ShouldNotProcess()
    {
        // Arrange
        var engine = new ExpanderEngine();
        var repository = new MockSnippetRepository();
        
        var category = new Category { Id = "cat1", Name = "Category", IsEnabled = true };
        repository.AddCategory(category);
        
        repository.AddSnippet(new Snippet
        {
            Id = "1",
            CategoryId = "cat1",
            Keyword = ";sig",
            Replacement = "Best regards",
            IsEnabled = true
        });
        
        engine.SetRepository(repository);
        engine.SetSendingFlag(true);

        bool eventTriggered = false;
        engine.OnExpansionNeeded += (_) => eventTriggered = true;

        // Act
        foreach (char ch in ";sig ")
        {
            engine.OnCharInput(ch);
        }

        // Assert
        Assert.False(eventTriggered);
    }

    [Fact]
    public void OnCharInput_WithMultipleDelimiters_ShouldMatchCorrectly()
    {
        // Arrange
        var engine = new ExpanderEngine();
        var repository = new MockSnippetRepository();
        
        var category = new Category { Id = "cat1", Name = "Category", IsEnabled = true };
        repository.AddCategory(category);
        
        repository.AddSnippet(new Snippet
        {
            Id = "1",
            CategoryId = "cat1",
            Keyword = ";addr",
            Replacement = "주소입니다",
            IsEnabled = true
        });
        
        engine.SetRepository(repository);

        int eventCount = 0;
        engine.OnExpansionNeeded += (_) => eventCount++;

        // Act
        foreach (char ch in "Hello ;addr.")
        {
            engine.OnCharInput(ch);
        }

        // Assert
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void OnCharInput_WithEmptyBuffer_ShouldNotTrigger()
    {
        // Arrange
        var engine = new ExpanderEngine();
        var repository = new MockSnippetRepository();
        engine.SetRepository(repository);

        bool eventTriggered = false;
        engine.OnExpansionNeeded += (_) => eventTriggered = true;

        // Act - 빈 버퍼 상태에서 구분자 입력
        engine.OnCharInput(' ');

        // Assert
        Assert.False(eventTriggered);
    }

    [Fact]
    public void OnCharInput_WithDisabledCategory_ShouldNotMatch()
    {
        // Arrange
        var engine = new ExpanderEngine();
        var repository = new MockSnippetRepository();
        
        var category = new Category { Id = "cat1", Name = "Category", IsEnabled = false }; // 비활성
        repository.AddCategory(category);
        
        repository.AddSnippet(new Snippet
        {
            Id = "1",
            CategoryId = "cat1",
            Keyword = ";test",
            Replacement = "테스트",
            IsEnabled = true
        });
        
        engine.SetRepository(repository);

        bool eventTriggered = false;
        engine.OnExpansionNeeded += (_) => eventTriggered = true;

        // Act
        foreach (char ch in ";test ")
        {
            engine.OnCharInput(ch);
        }

        // Assert
        Assert.False(eventTriggered);
    }
}

/// <summary>
/// 테스트용 Mock 저장소
/// </summary>
internal class MockSnippetRepository : ISnippetRepository
{
    private readonly List<Category> _categories = new();
    private readonly List<Snippet> _snippets = new();

    public void AddCategory(Category category) => _categories.Add(category);
    public void AddSnippet(Snippet snippet) => _snippets.Add(snippet);

    public Task<List<Category>> GetAllCategoriesAsync() => Task.FromResult(_categories);
    
    public Task<List<Snippet>> GetSnippetsByCategoryAsync(string categoryId) =>
        Task.FromResult(_snippets.Where(s => s.CategoryId == categoryId).ToList());
    
    public Task<List<Snippet>> GetActiveSnippetsAsync()
    {
        var activeCategories = _categories.Where(c => c.IsEnabled).Select(c => c.Id).ToHashSet();
        return Task.FromResult(_snippets.Where(s => s.IsEnabled && activeCategories.Contains(s.CategoryId)).ToList());
    }

    public Snippet? GetActiveSnippetByKeyword(string keyword)
    {
        var activeCategories = _categories.Where(c => c.IsEnabled).Select(c => c.Id).ToHashSet();
        return _snippets.FirstOrDefault(s =>
            s.IsEnabled &&
            s.Keyword == keyword &&
            activeCategories.Contains(s.CategoryId)
        );
    }

    public Task<Category> AddCategoryAsync(Category category)
    {
        _categories.Add(category);
        return Task.FromResult(category);
    }

    public Task UpdateCategoryAsync(Category category) => Task.CompletedTask;
    public Task DeleteCategoryAsync(string categoryId) => Task.CompletedTask;
    public Task<Snippet> AddSnippetAsync(Snippet snippet)
    {
        _snippets.Add(snippet);
        return Task.FromResult(snippet);
    }

    public Task UpdateSnippetAsync(Snippet snippet) => Task.CompletedTask;
    public Task DeleteSnippetAsync(string snippetId) => Task.CompletedTask;
    public Task SaveAsync() => Task.CompletedTask;
    public Task LoadAsync() => Task.CompletedTask;
}
