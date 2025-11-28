using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TextExpander.App.ViewModels;

/// <summary>
/// 카테고리 추가/수정 다이얼로그 뷰모델
/// </summary>
public partial class CategoryDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string categoryName = "새 카테고리";

    [ObservableProperty]
    private ColorPaletteItem? selectedColor;

    public ObservableCollection<ColorPaletteItem> ColorPalette { get; } = new();
    
    public bool IsEditMode { get; }

    public CategoryDialogViewModel(CategoryViewModel? existingCategory = null)
    {
        InitializeColorPalette();
        
        if (existingCategory != null)
        {
            IsEditMode = true;
            CategoryName = existingCategory.Name;
            
            // 기존 색상 찾기 및 선택 표시
            SelectedColor = ColorPalette.FirstOrDefault(c => 
                c.BackgroundColor == existingCategory.BackgroundColor && 
                c.ForegroundColor == existingCategory.ForegroundColor) ?? ColorPalette[0];
        }
        else
        {
            IsEditMode = false;
            SelectedColor = ColorPalette[0];
        }
        
        // 선택된 색상 표시 업데이트
        if (SelectedColor != null)
        {
            SelectedColor.IsSelected = true;
        }
    }

    partial void OnSelectedColorChanged(ColorPaletteItem? oldValue, ColorPaletteItem? newValue)
    {
        // 이전 선택 해제
        if (oldValue != null)
        {
            oldValue.IsSelected = false;
        }
        
        // 새 선택 표시
        if (newValue != null)
        {
            newValue.IsSelected = true;
        }
    }

    private void InitializeColorPalette()
    {
        ColorPalette.Add(new ColorPaletteItem("회색", "#E0E0E0", "#000000"));
        ColorPalette.Add(new ColorPaletteItem("파란색", "#E3F2FD", "#1565C0"));
        ColorPalette.Add(new ColorPaletteItem("초록색", "#E8F5E9", "#2E7D32"));
        ColorPalette.Add(new ColorPaletteItem("빨간색", "#FFEBEE", "#C62828"));
        ColorPalette.Add(new ColorPaletteItem("주황색", "#FFF3E0", "#E65100"));
        ColorPalette.Add(new ColorPaletteItem("보라색", "#F3E5F5", "#6A1B9A"));
        ColorPalette.Add(new ColorPaletteItem("청록색", "#E0F7FA", "#00695C"));
        ColorPalette.Add(new ColorPaletteItem("노란색", "#FFF9C4", "#F57F17"));
        ColorPalette.Add(new ColorPaletteItem("분홍색", "#FCE4EC", "#C2185B"));
        ColorPalette.Add(new ColorPaletteItem("남색", "#E8EAF6", "#283593"));
    }
}

/// <summary>
/// 색상 팔레트 아이템
/// </summary>
public partial class ColorPaletteItem : ObservableObject
{
    public string Name { get; }
    public string BackgroundColor { get; }
    public string ForegroundColor { get; }

    [ObservableProperty]
    private bool isSelected;

    public ColorPaletteItem(string name, string backgroundColor, string foregroundColor)
    {
        Name = name;
        BackgroundColor = backgroundColor;
        ForegroundColor = foregroundColor;
    }
}

