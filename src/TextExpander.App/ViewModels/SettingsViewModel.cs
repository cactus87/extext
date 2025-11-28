using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextExpander.App.Services;
using TextExpander.Core.Models;
using TextExpander.Core.Repositories;

namespace TextExpander.App.ViewModels;

/// <summary>
/// 설정 윈도우 뷰모델
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly Action<AppSettings> _onSettingsApplied;
    private readonly Action? _onWindowClose;
    private readonly AutoStartService _autoStartService;

    // 구분자 키 설정
    [ObservableProperty]
    private bool useTabAsDelimiter = true;

    [ObservableProperty]
    private bool usePeriodAsDelimiter = true;

    [ObservableProperty]
    private bool useCommaAsDelimiter = true;

    [ObservableProperty]
    private bool useSemicolonAsDelimiter = true;

    [ObservableProperty]
    private bool useBacktickAsDelimiter = true;

    [ObservableProperty]
    private bool useSingleQuoteAsDelimiter = true;

    [ObservableProperty]
    private bool useSlashAsDelimiter = true;

    // 효과음
    [ObservableProperty]
    private bool playSoundOnExpansion = false;

    // 폴링 간격
    [ObservableProperty]
    private int keyProcessingIntervalMs = 0;

    // 키보드 출력 딜레이
    [ObservableProperty]
    private int backspaceDelayMs = 10;

    [ObservableProperty]
    private int textCharDelayMs = 5;

    // 자동 시작
    [ObservableProperty]
    private bool autoStartEnabled = false;

    public SettingsViewModel(ISettingsRepository settingsRepository, Action<AppSettings> onSettingsApplied, Action? onWindowClose = null, AutoStartService? autoStartService = null)
    {
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _onSettingsApplied = onSettingsApplied ?? throw new ArgumentNullException(nameof(onSettingsApplied));
        _onWindowClose = onWindowClose;
        _autoStartService = autoStartService ?? new AutoStartService();
    }

    /// <summary>
    /// 설정 로드
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        var settings = await _settingsRepository.LoadAsync();
        
        UseTabAsDelimiter = settings.UseTabAsDelimiter;
        UsePeriodAsDelimiter = settings.UsePeriodAsDelimiter;
        UseCommaAsDelimiter = settings.UseCommaAsDelimiter;
        UseSemicolonAsDelimiter = settings.UseSemicolonAsDelimiter;
        UseBacktickAsDelimiter = settings.UseBacktickAsDelimiter;
        UseSingleQuoteAsDelimiter = settings.UseSingleQuoteAsDelimiter;
        UseSlashAsDelimiter = settings.UseSlashAsDelimiter;
        PlaySoundOnExpansion = settings.PlaySoundOnExpansion;
        KeyProcessingIntervalMs = settings.KeyProcessingIntervalMs;
        BackspaceDelayMs = settings.BackspaceDelayMs;
        TextCharDelayMs = settings.TextCharDelayMs;
        
        // 자동 시작 상태 로드
        AutoStartEnabled = _autoStartService.IsAutoStartEnabled();
    }

    /// <summary>
    /// 설정 적용
    /// </summary>
    [RelayCommand]
    private async Task ApplyAsync()
    {
        var settings = new AppSettings
        {
            UseTabAsDelimiter = UseTabAsDelimiter,
            UsePeriodAsDelimiter = UsePeriodAsDelimiter,
            UseCommaAsDelimiter = UseCommaAsDelimiter,
            UseSemicolonAsDelimiter = UseSemicolonAsDelimiter,
            UseBacktickAsDelimiter = UseBacktickAsDelimiter,
            UseSingleQuoteAsDelimiter = UseSingleQuoteAsDelimiter,
            UseSlashAsDelimiter = UseSlashAsDelimiter,
            PlaySoundOnExpansion = PlaySoundOnExpansion,
            KeyProcessingIntervalMs = KeyProcessingIntervalMs,
            BackspaceDelayMs = BackspaceDelayMs,
            TextCharDelayMs = TextCharDelayMs
        };

        await _settingsRepository.SaveAsync(settings);
        
        // 자동 시작 설정 적용
        try
        {
            if (AutoStartEnabled)
            {
                _autoStartService.EnableAutoStart();
            }
            else
            {
                _autoStartService.DisableAutoStart();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"자동 시작 설정 실패:\n{ex.Message}",
                "경고",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
        
        _onSettingsApplied?.Invoke(settings);
        _onWindowClose?.Invoke();
    }

    /// <summary>
    /// 설정 취소 (다시 로드)
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await LoadSettingsAsync();
        _onWindowClose?.Invoke();
    }
}

