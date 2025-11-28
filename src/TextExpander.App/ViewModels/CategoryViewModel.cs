using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TextExpander.App.ViewModels;

/// <summary>
/// 카테고리 뷰모델
/// </summary>
public partial class CategoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private string backgroundColor = "#E0E0E0";

    [ObservableProperty]
    private string foregroundColor = "#000000";

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime updatedAt;

    /// <summary>
    /// 배경색 Brush (WPF 바인딩용)
    /// </summary>
    public System.Windows.Media.Brush BackgroundBrush => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(BackgroundColor));

    /// <summary>
    /// 글자색 Brush (WPF 바인딩용)
    /// </summary>
    public System.Windows.Media.Brush ForegroundBrush => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ForegroundColor));
}

