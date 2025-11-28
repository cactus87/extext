using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TextExpander.App.ViewModels;

namespace TextExpander.App.Windows;

/// <summary>
/// MainWindow.xaml에 대한 상호 작용 논리
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetWindowIcon();
    }

    /// <summary>
    /// 윈도우 아이콘 설정
    /// </summary>
    private void SetWindowIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(iconPath))
            {
                Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
            }
        }
        catch
        {
            // 아이콘 로드 실패 시 기본 아이콘 사용
        }
    }

    private async void CategoryListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.LoadSnippetsCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// 카테고리 더블클릭 이벤트 (수정)
    /// </summary>
    private void CategoryListBox_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            // '전체' 카테고리는 수정 불가
            if (vm.SelectedCategory?.Id == "__ALL__")
            {
                return;
            }
            
            vm.EditCategoryCommand.Execute(null);
        }
    }
}

