using System.Text.Json;
using TextExpander.Core.Domain;
using TextExpander.Core.Models;

namespace TextExpander.Core.Repositories;

/// <summary>
/// JSON 파일 기반 스니펫 저장소 구현
/// %AppData%\TextExpander\snippets.json에 데이터 저장
/// </summary>
public class JsonSnippetRepository : ISnippetRepository
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TextExpander"
    );
    
    private static readonly string FilePath = Path.Combine(AppDataPath, "snippets.json");
    private static readonly string BackupPath = Path.Combine(AppDataPath, "snippets.json.bak");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private List<Category> _categories = new();
    private List<Snippet> _snippets = new();

    // ========== 조회 ==========

    /// <inheritdoc/>
    public Task<List<Category>> GetAllCategoriesAsync()
    {
        return Task.FromResult(new List<Category>(_categories));
    }

    /// <inheritdoc/>
    public Task<List<Snippet>> GetSnippetsByCategoryAsync(string categoryId)
    {
        var snippets = _snippets
            .Where(s => s.CategoryId == categoryId)
            .ToList();
        return Task.FromResult(snippets);
    }

    /// <inheritdoc/>
    public Task<List<Snippet>> GetActiveSnippetsAsync()
    {
        var activeCategories = _categories
            .Where(c => c.IsEnabled)
            .Select(c => c.Id)
            .ToHashSet();

        var activeSnippets = _snippets
            .Where(s => s.IsEnabled && activeCategories.Contains(s.CategoryId))
            .ToList();

        return Task.FromResult(activeSnippets);
    }

    /// <inheritdoc/>
    public Snippet? GetActiveSnippetByKeyword(string keyword)
    {
        var activeCategories = _categories
            .Where(c => c.IsEnabled)
            .Select(c => c.Id)
            .ToHashSet();

        return _snippets.FirstOrDefault(s =>
            s.IsEnabled &&
            s.Keyword == keyword &&
            activeCategories.Contains(s.CategoryId)
        );
    }

    // ========== CRUD - 카테고리 ==========

    /// <inheritdoc/>
    public Task<Category> AddCategoryAsync(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Id))
        {
            category.Id = Guid.NewGuid().ToString();
        }
        
        category.CreatedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;
        
        _categories.Add(category);
        return Task.FromResult(category);
    }

    /// <inheritdoc/>
    public Task UpdateCategoryAsync(Category category)
    {
        var existing = _categories.FirstOrDefault(c => c.Id == category.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"카테고리를 찾을 수 없습니다: {category.Id}");
        }

        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.IsEnabled = category.IsEnabled;
        existing.UpdatedAt = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteCategoryAsync(string categoryId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category != null)
        {
            _categories.Remove(category);
            
            // 해당 카테고리의 모든 스니펫도 삭제
            _snippets.RemoveAll(s => s.CategoryId == categoryId);
        }

        return Task.CompletedTask;
    }

    // ========== CRUD - 스니펫 ==========

    /// <inheritdoc/>
    public Task<Snippet> AddSnippetAsync(Snippet snippet)
    {
        if (string.IsNullOrWhiteSpace(snippet.Id))
        {
            snippet.Id = Guid.NewGuid().ToString();
        }
        
        snippet.CreatedAt = DateTime.UtcNow;
        snippet.UpdatedAt = DateTime.UtcNow;
        
        _snippets.Add(snippet);
        return Task.FromResult(snippet);
    }

    /// <inheritdoc/>
    public Task UpdateSnippetAsync(Snippet snippet)
    {
        var existing = _snippets.FirstOrDefault(s => s.Id == snippet.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"스니펫을 찾을 수 없습니다: {snippet.Id}");
        }

        existing.CategoryId = snippet.CategoryId;
        existing.Keyword = snippet.Keyword;
        existing.Replacement = snippet.Replacement;
        existing.IsEnabled = snippet.IsEnabled;
        existing.Note = snippet.Note;
        existing.UpdatedAt = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteSnippetAsync(string snippetId)
    {
        var snippet = _snippets.FirstOrDefault(s => s.Id == snippetId);
        if (snippet != null)
        {
            _snippets.Remove(snippet);
        }

        return Task.CompletedTask;
    }

    // ========== 저장/로드 ==========

    /// <inheritdoc/>
    public async Task SaveAsync()
    {
        try
        {
            // 디렉터리가 없으면 생성
            Directory.CreateDirectory(AppDataPath);

            var data = new SnippetData
            {
                Version = "1.0",
                LastUpdated = DateTime.UtcNow,
                Categories = _categories,
                Snippets = _snippets
            };

            // 임시 파일에 먼저 쓰기
            string tempPath = FilePath + ".tmp";
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(tempPath, json);

            // 기존 파일이 있으면 백업
            if (File.Exists(FilePath))
            {
                File.Copy(FilePath, BackupPath, overwrite: true);
            }

            // 임시 파일을 정식 파일로 교체 (원자적 연산)
            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("스니펫 데이터 저장 중 오류 발생", ex);
        }
    }

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                // 파일이 없으면 빈 상태로 시작
                _categories = new List<Category>();
                _snippets = new List<Snippet>();
                return;
            }

            string json = await File.ReadAllTextAsync(FilePath);
            var data = JsonSerializer.Deserialize<SnippetData>(json, _jsonOptions);

            if (data != null)
            {
                _categories = data.Categories;
                _snippets = data.Snippets;
            }
            else
            {
                _categories = new List<Category>();
                _snippets = new List<Snippet>();
            }
        }
        catch (JsonException)
        {
            // JSON 파싱 실패 시 백업에서 복구 시도
            await TryLoadFromBackupAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("스니펫 데이터 로드 중 오류 발생", ex);
        }
    }

    /// <summary>
    /// 백업 파일에서 데이터 로드 시도
    /// </summary>
    private async Task TryLoadFromBackupAsync()
    {
        try
        {
            if (File.Exists(BackupPath))
            {
                string json = await File.ReadAllTextAsync(BackupPath);
                var data = JsonSerializer.Deserialize<SnippetData>(json, _jsonOptions);

                if (data != null)
                {
                    _categories = data.Categories;
                    _snippets = data.Snippets;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            // 백업도 실패하면 빈 상태로 시작
            // TODO: 로깅 시스템 추가 시 기록
            // logger.LogWarning(ex, "백업 파일 복구 실패, 빈 상태로 초기화");
            _ = ex; // 예외 변수 사용 (컴파일러 경고 방지)
        }

        _categories = new List<Category>();
        _snippets = new List<Snippet>();
    }

    // ========== Import/Export ==========

    /// <summary>
    /// 외부 JSON 파일에서 스니펫 데이터 import
    /// 기존 데이터에 추가 또는 병합
    /// </summary>
    public async Task ImportFromFileAsync(string filePath, bool merge = true)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Import 파일을 찾을 수 없습니다: {filePath}");
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize<SnippetData>(json, _jsonOptions);

            if (data == null)
            {
                throw new InvalidOperationException("Import 파일 형식이 올바르지 않습니다");
            }

            if (!merge)
            {
                // 기존 데이터 전체 대체
                _categories = data.Categories ?? new List<Category>();
                _snippets = data.Snippets ?? new List<Snippet>();
            }
            else
            {
                // 기존 데이터와 병합
                MergeCategories(data.Categories ?? new List<Category>());
                MergeSnippets(data.Snippets ?? new List<Snippet>());
            }

            await SaveAsync();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Import 파일 JSON 파싱 실패", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("데이터 import 중 오류 발생", ex);
        }
    }

    /// <summary>
    /// 현재 데이터를 파일로 export
    /// </summary>
    public async Task ExportToFileAsync(string filePath)
    {
        try
        {
            var data = new SnippetData
            {
                Version = "1.0",
                LastUpdated = DateTime.UtcNow,
                Categories = _categories,
                Snippets = _snippets
            };

            string json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("데이터 export 중 오류 발생", ex);
        }
    }

    /// <summary>
    /// 카테고리 병합 로직
    /// ID가 같은 카테고리는 유지, 새로운 카테고리만 추가
    /// </summary>
    private void MergeCategories(List<Category> importedCategories)
    {
        var existingIds = _categories.Select(c => c.Id).ToHashSet();

        foreach (var category in importedCategories)
        {
            if (!existingIds.Contains(category.Id))
            {
                _categories.Add(category);
            }
        }
    }

    /// <summary>
    /// 스니펫 병합 로직
    /// 키워드 충돌 시 import된 데이터로 업데이트
    /// </summary>
    private void MergeSnippets(List<Snippet> importedSnippets)
    {
        foreach (var imported in importedSnippets)
        {
            // 같은 키워드와 카테고리를 가진 기존 스니펫 검색
            var existing = _snippets.FirstOrDefault(s =>
                s.Keyword == imported.Keyword &&
                s.CategoryId == imported.CategoryId
            );

            if (existing != null)
            {
                // 기존 스니펫 업데이트
                existing.Replacement = imported.Replacement;
                existing.IsEnabled = imported.IsEnabled;
                existing.Note = imported.Note;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // 새 스니펫 추가
                _snippets.Add(imported);
            }
        }
    }
}

