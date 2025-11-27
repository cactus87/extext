# TextExpander UI 개선 계획서

## 요구사항 요약

| # | 기능 | 설명 | 난이도 |
|---|------|------|--------|
| 1 | 카테고리 수정 | 더블클릭 수정 + 수정 버튼 추가 | 중 |
| 2 | 색상 선택 시각화 | 선택된 색상 강조 표시 | 낮음 |
| 3 | 색상 팔레트 확장 | 2열 레이아웃, 10개 색상 | 낮음 |
| 4 | 전체 체크박스 3-상태 | 반전 → 전체선택 → 전체해제 순환 | 중 |
| 5 | 앱 아이콘 설정 | 트레이 + 윈도우 아이콘 | 낮음 |
| 6 | 한/영 모드 무관 트리거 | VK 코드 기반 (이미 구현됨, 확인만) | 낮음 |

---

## 1단계: 카테고리 수정 기능

### 수정 파일
- `src/TextExpander.App/Windows/MainWindow.xaml`
- `src/TextExpander.App/Windows/MainWindow.xaml.cs`
- `src/TextExpander.App/ViewModels/MainWindowViewModel.cs`
- `src/TextExpander.App/Windows/CategoryDialog.xaml`
- `src/TextExpander.App/Windows/CategoryDialog.xaml.cs`

### 변경 내용

#### 1-1. 수정 버튼 추가
```xml
<!-- MainWindow.xaml - 카테고리 버튼 영역 -->
<StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,5,0,0">
    <Button Content="추가" Command="{Binding AddCategoryCommand}" Width="50" Margin="0,0,5,0"/>
    <Button Content="수정" Command="{Binding EditCategoryCommand}" Width="50" Margin="0,0,5,0"/>
    <Button Content="삭제" Command="{Binding DeleteCategoryCommand}" Width="50"/>
</StackPanel>
```

#### 1-2. 더블클릭 이벤트 추가
```xml
<!-- MainWindow.xaml - ListBox -->
<ListBox ... MouseDoubleClick="CategoryListBox_DoubleClick">
```

#### 1-3. MainWindowViewModel에 EditCategoryCommand 추가
```csharp
[RelayCommand]
private async Task EditCategoryAsync()
{
    if (SelectedCategory == null || SelectedCategory.Id == "__ALL__") return;
    
    var dialog = new CategoryDialog(SelectedCategory);  // 기존 데이터 전달
    dialog.ShowDialog();
    
    if (dialog.DialogResult)
    {
        // 업데이트 로직
    }
}
```

#### 1-4. CategoryDialog 수정 모드 지원
```csharp
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
```

---

## 2단계: 색상 선택 시각화

### 수정 파일
- `src/TextExpander.App/Windows/CategoryDialog.xaml`
- `src/TextExpander.App/ViewModels/CategoryDialogViewModel.cs`

### 변경 내용

#### 2-1. ColorPaletteItem에 IsSelected 속성 추가
```csharp
public class ColorPaletteItem : ObservableObject
{
    public string Name { get; }
    public string BackgroundColor { get; }
    public string ForegroundColor { get; }
    
    [ObservableProperty]
    private bool isSelected;
}
```

#### 2-2. 선택 시 시각적 표시 (XAML)
```xml
<Border BorderBrush="{Binding IsSelected, Converter={StaticResource BoolToBorderConverter}}"
        BorderThickness="3">
    <!-- 또는 체크 아이콘 표시 -->
    <TextBlock Text="✓" Visibility="{Binding IsSelected, Converter={StaticResource BoolToVisibilityConverter}}"
               FontSize="20" Foreground="Green"/>
</Border>
```

---

## 3단계: 색상 팔레트 확장 (2열, 10개)

### 수정 파일
- `src/TextExpander.App/Windows/CategoryDialog.xaml`
- `src/TextExpander.App/ViewModels/CategoryDialogViewModel.cs`

### 변경 내용

#### 3-1. 색상 10개로 조정
```csharp
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
```

#### 3-2. 2열 레이아웃
```xml
<ItemsControl ItemsSource="{Binding ColorPalette}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <UniformGrid Columns="2"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <!-- ItemTemplate은 기존과 동일 -->
</ItemsControl>
```

---

## 4단계: '전체' 체크박스 3-상태 순환

### 수정 파일
- `src/TextExpander.App/ViewModels/MainWindowViewModel.cs`
- `src/TextExpander.App/Windows/MainWindow.xaml`

### 동작 로직
```
상태 0 (초기): 개별 체크박스 상태 유지
클릭 1회: 모든 체크박스 반전
클릭 2회: 모든 체크박스 체크 (전체 선택)
클릭 3회: 모든 체크박스 해제 (전체 해제)
→ 다시 클릭 1회로 순환
```

### 변경 내용

#### 4-1. 상태 변수 추가
```csharp
private int _allCategoryClickState = 0;  // 0, 1, 2 순환
```

#### 4-2. '전체' 카테고리 체크박스 클릭 처리
```csharp
private void HandleAllCategoryToggle()
{
    _allCategoryClickState = (_allCategoryClickState + 1) % 3;
    
    switch (_allCategoryClickState)
    {
        case 0:  // 반전
            foreach (var cat in Categories.Where(c => c.Id != "__ALL__"))
                cat.IsEnabled = !cat.IsEnabled;
            break;
        case 1:  // 전체 선택
            foreach (var cat in Categories.Where(c => c.Id != "__ALL__"))
                cat.IsEnabled = true;
            break;
        case 2:  // 전체 해제
            foreach (var cat in Categories.Where(c => c.Id != "__ALL__"))
                cat.IsEnabled = false;
            break;
    }
}
```

---

## 5단계: 앱 아이콘 설정

### 작업 내용
1. 아이콘 파일 복사: `C:\Users\user\Desktop\1469d061\1-1c3220a3.ico` → `src/TextExpander.App/Resources/app.ico`
2. 프로젝트 파일 수정
3. 트레이 아이콘 적용

### 수정 파일
- `src/TextExpander.App/TextExpander.App.csproj`
- `src/TextExpander.App/App.xaml`
- `src/TextExpander.App/Services/TrayIconService.cs`

#### 5-1. 프로젝트 파일에 아이콘 추가
```xml
<!-- TextExpander.App.csproj -->
<PropertyGroup>
    <ApplicationIcon>Resources\app.ico</ApplicationIcon>
</PropertyGroup>

<ItemGroup>
    <Resource Include="Resources\app.ico" />
</ItemGroup>
```

#### 5-2. 윈도우 아이콘 설정
```xml
<!-- MainWindow.xaml -->
<Window ... Icon="/Resources/app.ico">
```

#### 5-3. 트레이 아이콘 설정
```csharp
// TrayIconService.cs
_notifyIcon.Icon = new System.Drawing.Icon("Resources/app.ico");
```

---

## 6단계: 한/영 모드 무관 트리거 (확인)

### 현재 상태
- `GlobalKeyboardHook`이 VK 코드 기반으로 동작
- `MapVirtualKey`로 영문 문자 변환

### 확인 사항
```csharp
// GlobalKeyboardHook.cs - 현재 코드 확인
char ch = (char)MapVirtualKey((uint)vkCode, MAPVK_VK_TO_CHAR);
```

### 예상 결과
- 한글 모드에서 `;home` 키 입력 → 화면: `;ㅗㅐㅡㄷ` → 버퍼: `;home` → 매칭 성공!
- 추가 작업 없음 (이미 동작해야 함)

### 테스트 방법
1. 영문 키워드 `;test` 등록
2. 한글 모드로 전환
3. `;test` 키 입력 (화면에는 `;ㅅㄷㄴㅅ`)
4. 스니펫 확장 확인

---

## 구현 순서 (권장)

| 순서 | 단계 | 예상 시간 |
|------|------|----------|
| 1 | 5단계: 앱 아이콘 | 10분 |
| 2 | 6단계: 한/영 확인 | 5분 (테스트만) |
| 3 | 3단계: 색상 팔레트 확장 | 15분 |
| 4 | 2단계: 색상 선택 시각화 | 20분 |
| 5 | 4단계: 전체 체크박스 3-상태 | 30분 |
| 6 | 1단계: 카테고리 수정 | 40분 |

**총 예상 시간: 약 2시간**

---

## 파일 변경 요약

| 파일 | 변경 내용 |
|------|----------|
| `MainWindow.xaml` | 수정 버튼, 더블클릭 이벤트 |
| `MainWindow.xaml.cs` | 더블클릭 핸들러 |
| `MainWindowViewModel.cs` | EditCategoryCommand, 3-상태 체크박스 |
| `CategoryDialog.xaml` | 2열 레이아웃, 선택 표시 |
| `CategoryDialog.xaml.cs` | 수정 모드 생성자 |
| `CategoryDialogViewModel.cs` | 수정 모드, IsSelected |
| `TextExpander.App.csproj` | 아이콘 설정 |
| `TrayIconService.cs` | 트레이 아이콘 |
| `GlobalKeyboardHook.cs` | 확인만 (변경 없음 예상) |

