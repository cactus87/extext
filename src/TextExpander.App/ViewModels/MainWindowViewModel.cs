using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TextExpander.Core.Domain;
using TextExpander.Core.Engine;
using TextExpander.Core.Models;
using TextExpander.Core.Repositories;
using Win32MessageBox = System.Windows.MessageBox;
using Win32SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Win32OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TextExpander.App.ViewModels;

/// <summary>
/// 메인 윈도우 뷰모델
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ISnippetRepository _repository;
    private readonly IExpanderEngine _engine;
    private readonly Action _openSettingsWindowAction;
    private int _allCategoryClickState = 0;  // 0: 반전, 1: 전체선택, 2: 전체해제

    [ObservableProperty]
    private ObservableCollection<CategoryViewModel> categories = new();

    [ObservableProperty]
    private CategoryViewModel? selectedCategory;

    [ObservableProperty]
    private ObservableCollection<SnippetViewModel> snippets = new();

    [ObservableProperty]
    private ObservableCollection<SnippetViewModel> filteredSnippets = new();

    [ObservableProperty]
    private SnippetViewModel? selectedSnippet;

    [ObservableProperty]
    private bool isEngineEnabled = true;

    [ObservableProperty]
    private string statusText = "동작 중";

    [ObservableProperty]
    private string searchText = string.Empty;

    public MainWindowViewModel(ISnippetRepository repository, IExpanderEngine engine, Action openSettingsWindowAction)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _openSettingsWindowAction = openSettingsWindowAction ?? throw new ArgumentNullException(nameof(openSettingsWindowAction));
        
        // 검색 텍스트 변경 시 필터링
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchText) || e.PropertyName == nameof(Snippets))
            {
                FilterSnippets();
            }
        };
        
        // 카테고리 활성 상태 변경 감지
        Categories.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (CategoryViewModel cat in e.NewItems)
                {
                    cat.PropertyChanged += Category_PropertyChanged;
                }
            }
        };
    }

    /// <summary>
    /// 카테고리 속성 변경 이벤트 핸들러
    /// </summary>
    private async void Category_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CategoryViewModel.IsEnabled) && sender is CategoryViewModel category)
        {
            // '전체' 카테고리는 3-상태 순환 처리
            if (category.Id == "__ALL__")
            {
                HandleAllCategoryToggle();
                return;
            }

            await HandleCategoryToggleAsync(category);
        }
    }

    /// <summary>
    /// '전체' 카테고리 3-상태 순환 처리
    /// </summary>
    private async void HandleAllCategoryToggle()
    {
        _allCategoryClickState = (_allCategoryClickState + 1) % 3;
        
        var realCategories = Categories.Where(c => c.Id != "__ALL__").ToList();
        
        switch (_allCategoryClickState)
        {
            case 0:  // 반전
                foreach (var cat in realCategories)
                {
                    cat.IsEnabled = !cat.IsEnabled;
                }
                StatusText = "카테고리 활성 상태 반전";
                break;
                
            case 1:  // 전체 선택
                foreach (var cat in realCategories)
                {
                    cat.IsEnabled = true;
                }
                StatusText = "모든 카테고리 활성화";
                break;
                
            case 2:  // 전체 해제
                foreach (var cat in realCategories)
                {
                    cat.IsEnabled = false;
                }
                StatusText = "모든 카테고리 비활성화";
                break;
        }
        
        // 변경사항 저장
        await ApplyChangesAsync();
        
        await Task.Delay(1500);
        StatusText = IsEngineEnabled ? "동작 중" : "일시 정지";
    }

    /// <summary>
    /// 카테고리 활성/비활성 처리
    /// </summary>
    private async Task HandleCategoryToggleAsync(CategoryViewModel category)
    {
        try
        {
            // 해당 카테고리의 모든 스니펫 가져오기
            var snippets = await _repository.GetSnippetsByCategoryAsync(category.Id);
            
            // 스니펫 활성 상태 변경
            foreach (var snippet in snippets)
            {
                snippet.IsEnabled = category.IsEnabled;
                await _repository.UpdateSnippetAsync(snippet);
            }
            
            await _repository.SaveAsync();
            
            // 현재 선택된 카테고리가 변경된 카테고리인 경우 다시 로드
            if (SelectedCategory?.Id == category.Id)
            {
                await LoadSnippetsAsync();
            }
            
            StatusText = category.IsEnabled 
                ? $"'{category.Name}' 카테고리 활성화됨" 
                : $"'{category.Name}' 카테고리 비활성화됨";
            await Task.Delay(1500);
            StatusText = IsEngineEnabled ? "동작 중" : "일시 정지";
        }
        catch (Exception ex)
        {
            StatusText = $"카테고리 토글 실패: {ex.Message}";
        }
    }

    /// <summary>
    /// 초기 데이터 로드
    /// </summary>
    public async Task LoadDataAsync()
    {
        await LoadCategoriesAsync();
    }

    /// <summary>
    /// 카테고리 목록 로드
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        var cats = await _repository.GetAllCategoriesAsync();
        Categories.Clear();
        
        // '전체' 가상 카테고리 추가
        Categories.Add(new CategoryViewModel
        {
            Id = "__ALL__",
            Name = "전체",
            IsEnabled = true,
            BackgroundColor = "#F5F5F5",
            ForegroundColor = "#000000",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        
        foreach (var cat in cats)
        {
            Categories.Add(new CategoryViewModel
            {
                Id = cat.Id,
                Name = cat.Name,
                Description = cat.Description,
                IsEnabled = cat.IsEnabled,
                BackgroundColor = cat.BackgroundColor,
                ForegroundColor = cat.ForegroundColor,
                CreatedAt = cat.CreatedAt,
                UpdatedAt = cat.UpdatedAt
            });
        }
    }

    /// <summary>
    /// 선택된 카테고리의 스니펫 로드
    /// </summary>
    [RelayCommand]
    private async Task LoadSnippetsAsync()
    {
        if (SelectedCategory == null)
        {
            Snippets.Clear();
            return;
        }

        // '전체' 카테고리인 경우
        if (SelectedCategory.Id == "__ALL__")
        {
            var allSnippets = new List<Snippet>();
            var categories = await _repository.GetAllCategoriesAsync();
            
            foreach (var cat in categories)
            {
                var snips = await _repository.GetSnippetsByCategoryAsync(cat.Id);
                allSnippets.AddRange(snips);
            }
            
            Snippets.Clear();
            foreach (var snip in allSnippets)
            {
                // 해당 카테고리 찾기
                var category = categories.FirstOrDefault(c => c.Id == snip.CategoryId);
                
                Snippets.Add(new SnippetViewModel
                {
                    Id = snip.Id,
                    CategoryId = snip.CategoryId,
                    Keyword = snip.Keyword,
                    Replacement = snip.Replacement,
                    IsEnabled = snip.IsEnabled,
                    Note = snip.Note,
                    CreatedAt = snip.CreatedAt,
                    UpdatedAt = snip.UpdatedAt,
                    CategoryBackgroundColor = category?.BackgroundColor ?? "#FFFFFF",
                    CategoryForegroundColor = category?.ForegroundColor ?? "#000000"
                });
            }
        }
        else
        {
            // 특정 카테고리의 스니펫 로드
            var snips = await _repository.GetSnippetsByCategoryAsync(SelectedCategory.Id);
            Snippets.Clear();
            
            foreach (var snip in snips)
            {
                Snippets.Add(new SnippetViewModel
                {
                    Id = snip.Id,
                    CategoryId = snip.CategoryId,
                    Keyword = snip.Keyword,
                    Replacement = snip.Replacement,
                    IsEnabled = snip.IsEnabled,
                    Note = snip.Note,
                    CreatedAt = snip.CreatedAt,
                    UpdatedAt = snip.UpdatedAt,
                    CategoryBackgroundColor = SelectedCategory.BackgroundColor,
                    CategoryForegroundColor = SelectedCategory.ForegroundColor
                });
            }
        }
        
        FilterSnippets();
    }

    /// <summary>
    /// 스니펫 필터링 (검색)
    /// </summary>
    private void FilterSnippets()
    {
        FilteredSnippets.Clear();
        
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // 검색어가 없으면 모든 스니펫 표시
            foreach (var snippet in Snippets)
            {
                FilteredSnippets.Add(snippet);
            }
        }
        else
        {
            // 키워드 또는 대체 텍스트에서 검색
            var searchLower = SearchText.ToLowerInvariant();
            foreach (var snippet in Snippets)
            {
                if (snippet.Keyword.ToLowerInvariant().Contains(searchLower) ||
                    snippet.Replacement.ToLowerInvariant().Contains(searchLower) ||
                    (snippet.Note?.ToLowerInvariant().Contains(searchLower) == true))
                {
                    FilteredSnippets.Add(snippet);
                }
            }
        }
    }

    /// <summary>
    /// 카테고리 추가
    /// </summary>
    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        var dialog = new Windows.CategoryDialog
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        dialog.ShowDialog();

        if (dialog.DialogResult && dialog.ViewModel.SelectedColor != null)
        {
            var newCategory = new Category
            {
                Name = dialog.ViewModel.CategoryName,
                Description = "",
                IsEnabled = true,
                BackgroundColor = dialog.ViewModel.SelectedColor.BackgroundColor,
                ForegroundColor = dialog.ViewModel.SelectedColor.ForegroundColor
            };

            await _repository.AddCategoryAsync(newCategory);
            await _repository.SaveAsync();
            await LoadCategoriesAsync();
            
            StatusText = $"카테고리 '{newCategory.Name}' 추가됨";
            await Task.Delay(1500);
            StatusText = IsEngineEnabled ? "동작 중" : "일시 정지";
        }
    }

    /// <summary>
    /// 카테고리 수정
    /// </summary>
    [RelayCommand]
    private async Task EditCategoryAsync()
    {
        if (SelectedCategory == null) return;

        // '전체' 카테고리는 수정 불가
        if (SelectedCategory.Id == "__ALL__")
        {
            Win32MessageBox.Show("'전체' 카테고리는 수정할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Windows.CategoryDialog(SelectedCategory)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        dialog.ShowDialog();

        if (dialog.DialogResult && dialog.ViewModel.SelectedColor != null)
        {
            var updatedCategory = new Category
            {
                Id = SelectedCategory.Id,
                Name = dialog.ViewModel.CategoryName,
                Description = SelectedCategory.Description,
                IsEnabled = SelectedCategory.IsEnabled,
                BackgroundColor = dialog.ViewModel.SelectedColor.BackgroundColor,
                ForegroundColor = dialog.ViewModel.SelectedColor.ForegroundColor,
                CreatedAt = SelectedCategory.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.UpdateCategoryAsync(updatedCategory);
            await _repository.SaveAsync();
            await LoadCategoriesAsync();
            
            // 수정된 카테고리 다시 선택
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == updatedCategory.Id);
            
            StatusText = $"카테고리 '{updatedCategory.Name}' 수정됨";
            await Task.Delay(1500);
            StatusText = IsEngineEnabled ? "동작 중" : "일시 정지";
        }
    }

    /// <summary>
    /// 카테고리 삭제
    /// </summary>
    [RelayCommand]
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null) return;

        // '전체' 카테고리는 삭제 불가
        if (SelectedCategory.Id == "__ALL__")
        {
            Win32MessageBox.Show("'전체' 카테고리는 삭제할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _repository.DeleteCategoryAsync(SelectedCategory.Id);
        await _repository.SaveAsync();
        await LoadCategoriesAsync();
        Snippets.Clear();
    }

    /// <summary>
    /// 스니펫 추가
    /// </summary>
    [RelayCommand]
    private async Task AddSnippetAsync()
    {
        if (SelectedCategory == null) return;

        // '전체' 카테고리가 선택된 경우 스니펫 추가 불가
        if (SelectedCategory.Id == "__ALL__")
        {
            Win32MessageBox.Show("'전체' 카테고리에는 스니펫을 추가할 수 없습니다.\n특정 카테고리를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var newSnippet = new Snippet
        {
            CategoryId = SelectedCategory.Id,
            Keyword = ";new",
            Replacement = "새 스니펫 내용",
            IsEnabled = true,
            Note = ""
        };

        await _repository.AddSnippetAsync(newSnippet);
        await _repository.SaveAsync();
        await LoadSnippetsAsync();
    }

    /// <summary>
    /// 스니펫 삭제
    /// </summary>
    [RelayCommand]
    private async Task DeleteSnippetAsync()
    {
        if (SelectedSnippet == null) return;

        await _repository.DeleteSnippetAsync(SelectedSnippet.Id);
        await _repository.SaveAsync();
        await LoadSnippetsAsync();
    }

    /// <summary>
    /// 엔진 토글 (활성화/비활성화)
    /// </summary>
    [RelayCommand]
    private void ToggleEngine()
    {
        IsEngineEnabled = !IsEngineEnabled;
        
        if (IsEngineEnabled)
        {
            _engine.Enable();
            StatusText = "동작 중";
        }
        else
        {
            _engine.Disable();
            StatusText = "일시 정지";
        }
    }

    /// <summary>
    /// 변경 사항 적용
    /// </summary>
    [RelayCommand]
    private async Task ApplyChangesAsync()
    {
        try
        {
            // 카테고리 업데이트 ('전체' 카테고리 제외)
            foreach (var catVM in Categories.Where(c => c.Id != "__ALL__"))
            {
                var category = new Category
                {
                    Id = catVM.Id,
                    Name = catVM.Name,
                    Description = catVM.Description,
                    IsEnabled = catVM.IsEnabled,
                    BackgroundColor = catVM.BackgroundColor,
                    ForegroundColor = catVM.ForegroundColor,
                    CreatedAt = catVM.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };
                await _repository.UpdateCategoryAsync(category);
            }

            // 스니펫 업데이트 (FilteredSnippets가 아닌 Snippets 사용)
            foreach (var snipVM in Snippets)
            {
                var snippet = new Snippet
                {
                    Id = snipVM.Id,
                    CategoryId = snipVM.CategoryId,
                    Keyword = snipVM.Keyword,
                    Replacement = snipVM.Replacement,
                    IsEnabled = snipVM.IsEnabled,
                    Note = snipVM.Note,
                    CreatedAt = snipVM.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };
                await _repository.UpdateSnippetAsync(snippet);
            }

            await _repository.SaveAsync();
            StatusText = "적용 완료! ✓";
            
            // 2초 후 원래 상태로
            await Task.Delay(2000);
            StatusText = IsEngineEnabled ? "동작 중" : "일시 정지";
        }
        catch (Exception ex)
        {
            StatusText = $"적용 실패: {ex.Message}";
        }
    }

    /// <summary>
    /// 변경 사항 취소 (다시 로드)
    /// </summary>
    [RelayCommand]
    private async Task CancelChangesAsync()
    {
        await LoadDataAsync();
        StatusText = "변경 취소됨";
        
        await Task.Delay(1500);
        StatusText = IsEngineEnabled ? "동작 중" : "일시 정지";
    }

    /// <summary>
    /// 설정 창 열기
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        _openSettingsWindowAction?.Invoke();
    }

    /// <summary>
    /// 스니펫 내보내기 (JSON 백업)
    /// </summary>
    [RelayCommand]
    private async Task ExportSnippetsAsync()
    {
        try
        {
            var saveDialog = new Win32SaveFileDialog
            {
                Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                FileName = $"TextExpander_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                DefaultExt = "json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                // 현재 데이터 가져오기
                var categories = await _repository.GetAllCategoriesAsync();
                var allSnippets = new List<Snippet>();
                
                foreach (var category in categories)
                {
                    var snippets = await _repository.GetSnippetsByCategoryAsync(category.Id);
                    allSnippets.AddRange(snippets);
                }

                var data = new SnippetData
                {
                    Version = "1.0",
                    LastUpdated = DateTime.UtcNow,
                    Categories = categories,
                    Snippets = allSnippets
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(data, jsonOptions);
                await File.WriteAllTextAsync(saveDialog.FileName, json);

                StatusText = $"내보내기 완료: {Path.GetFileName(saveDialog.FileName)}";
                await Task.Delay(2000);
                StatusText = IsEngineEnabled ? "동작 중" : "일시 정지";
            }
        }
        catch (Exception ex)
        {
            Win32MessageBox.Show($"내보내기 실패:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = $"내보내기 실패: {ex.Message}";
        }
    }

    /// <summary>
    /// 스니펫 가져오기 (JSON 복원)
    /// </summary>
    [RelayCommand]
    private async Task ImportSnippetsAsync()
    {
        try
        {
            var openDialog = new Win32OpenFileDialog
            {
                Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openDialog.ShowDialog() == true)
            {
                var result = Win32MessageBox.Show(
                    "기존 데이터를 모두 교체하시겠습니까?\n\n예: 기존 데이터 삭제 후 가져오기\n아니오: 기존 데이터에 병합",
                    "가져오기 확인",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                    return;

                // JSON 파일 읽기
                string json = await File.ReadAllTextAsync(openDialog.FileName);
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var data = JsonSerializer.Deserialize<SnippetData>(json, jsonOptions);
                
                if (data == null)
                {
                    Win32MessageBox.Show("잘못된 JSON 파일 형식입니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (result == MessageBoxResult.Yes)
                {
                    // 기존 데이터 모두 삭제
                    var existingCategories = await _repository.GetAllCategoriesAsync();
                    foreach (var category in existingCategories)
                    {
                        await _repository.DeleteCategoryAsync(category.Id);
                    }
                }

                // 카테고리 가져오기
                foreach (var category in data.Categories)
                {
                    if (result == MessageBoxResult.No)
                    {
                        // 병합 모드: 기존 카테고리 확인
                        var existing = await _repository.GetAllCategoriesAsync();
                        if (existing.Any(c => c.Id == category.Id))
                        {
                            await _repository.UpdateCategoryAsync(category);
                        }
                        else
                        {
                            await _repository.AddCategoryAsync(category);
                        }
                    }
                    else
                    {
                        await _repository.AddCategoryAsync(category);
                    }
                }

                // 스니펫 가져오기
                foreach (var snippet in data.Snippets)
                {
                    if (result == MessageBoxResult.No)
                    {
                        // 병합 모드: 기존 스니펫 확인
                        var existing = await _repository.GetSnippetsByCategoryAsync(snippet.CategoryId);
                        if (existing.Any(s => s.Id == snippet.Id))
                        {
                            await _repository.UpdateSnippetAsync(snippet);
                        }
                        else
                        {
                            await _repository.AddSnippetAsync(snippet);
                        }
                    }
                    else
                    {
                        await _repository.AddSnippetAsync(snippet);
                    }
                }

                await _repository.SaveAsync();
                await LoadDataAsync();

                StatusText = $"가져오기 완료: {Path.GetFileName(openDialog.FileName)}";
                await Task.Delay(2000);
                StatusText = IsEngineEnabled ? "동작 중" : "일시 정지";
            }
        }
        catch (Exception ex)
        {
            Win32MessageBox.Show($"가져오기 실패:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = $"가져오기 실패: {ex.Message}";
        }
    }
}

