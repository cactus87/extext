using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using TextExpander.App.ViewModels;

namespace TextExpander.App.Windows;

/// <summary>
/// 카테고리 추가/수정 다이얼로그
/// </summary>
public partial class CategoryDialog : Window
{
    public CategoryDialogViewModel ViewModel { get; }
    public new bool DialogResult { get; private set; }

    public CategoryDialog(CategoryViewModel? existingCategory = null)
    {
        InitializeComponent();
        ViewModel = new CategoryDialogViewModel(existingCategory);
        DataContext = ViewModel;
        
        if (existingCategory != null)
        {
            Title = "카테고리 수정";
        }
    }

    private void ColorItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is ColorPaletteItem colorItem)
        {
            ViewModel.SelectedColor = colorItem;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.CategoryName))
        {
            System.Windows.MessageBox.Show("카테고리 이름을 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ViewModel.SelectedColor == null)
        {
            System.Windows.MessageBox.Show("색상을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

