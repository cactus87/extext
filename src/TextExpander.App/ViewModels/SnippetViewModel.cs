using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TextExpander.App.ViewModels;

/// <summary>
/// 스니펫 뷰모델
/// </summary>
public partial class SnippetViewModel : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string categoryId = string.Empty;

    [ObservableProperty]
    private string keyword = string.Empty;

    [ObservableProperty]
    private string replacement = string.Empty;

    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private string note = string.Empty;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime updatedAt;

    [ObservableProperty]
    private string categoryBackgroundColor = "#FFFFFF";

    [ObservableProperty]
    private string categoryForegroundColor = "#000000";

    /// <summary>
    /// 카테고리 배경색 Brush (WPF 바인딩용)
    /// </summary>
    public System.Windows.Media.Brush CategoryBackgroundBrush => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(CategoryBackgroundColor));

    /// <summary>
    /// 카테고리 글자색 Brush (WPF 바인딩용)
    /// </summary>
    public System.Windows.Media.Brush CategoryForegroundBrush => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(CategoryForegroundColor));
}

