using System.Windows;
using System.IO;
using TextExpander.App.Services;
using TextExpander.App.ViewModels;
using TextExpander.App.Windows;
using TextExpander.Core.Engine;
using TextExpander.Core.Models;
using TextExpander.Core.Repositories;

namespace TextExpander.App;

/// <summary>
/// TextExpander 애플리케이션 진입점
/// 의존성 주입 및 서비스 초기화 담당
/// </summary>
public partial class App : System.Windows.Application
{
    private ISnippetRepository? _repository;
    private ISettingsRepository? _settingsRepository;
    private IExpanderEngine? _engine;
    private GlobalKeyboardHook? _keyboardHook;
    private KeyboardOutputService? _keyboardOutputService;
    private SoundService? _soundService;
    private AutoStartService? _autoStartService;
    private TrayIconService? _trayIconService;
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _viewModel;
    private AppSettings? _currentSettings;
    private System.IO.StreamWriter? _logWriter;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 디버그 로그 파일 초기화
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "textexpander-debug.txt");
        _logWriter = new System.IO.StreamWriter(logPath, append: true) { AutoFlush = true };
        _logWriter.WriteLine($"\n=== TextExpander Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

        try
        {
            await InitializeServicesAsync();
            SetupEventHandlers();
            ShowMainWindow();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"애플리케이션 시작 중 오류가 발생했습니다:\n{ex.Message}",
                "TextExpander 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// 서비스 초기화
    /// </summary>
    private async Task InitializeServicesAsync()
    {
        // 1. Core 저장소 초기화
        _repository = new JsonSnippetRepository();
        await _repository.LoadAsync();

        _settingsRepository = new JsonSettingsRepository();
        _currentSettings = await _settingsRepository.LoadAsync();

        // 2. 엔진 초기화 및 설정 적용
        _engine = new ExpanderEngine();
        _engine.SetRepository(_repository);
        _engine.SetSettings(_currentSettings);
        ((ExpanderEngine)_engine).SetLogWriter((msg) => _logWriter?.WriteLine(msg));

        // 3. App 서비스 초기화
        _keyboardOutputService = new KeyboardOutputService();
        _keyboardOutputService.SetSettings(_currentSettings);
        _soundService = new SoundService();
        _soundService.SetEnabled(_currentSettings.PlaySoundOnExpansion);
        _autoStartService = new AutoStartService();

        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.IsSendingChecker = () => _keyboardOutputService.IsSending;

        // 4. 트레이 아이콘 초기화
        _trayIconService = new TrayIconService(
            onShowWindow: ShowMainWindow,
            onTogglePause: ToggleEngine,
            onExit: () => Shutdown()
        );
        _trayIconService.Initialize();

        // 5. ViewModel 초기화
        _viewModel = new MainWindowViewModel(_repository, _engine, OpenSettingsWindow);
        await _viewModel.LoadDataAsync();
    }

    /// <summary>
    /// 이벤트 핸들러 설정
    /// </summary>
    private void SetupEventHandlers()
    {
        if (_keyboardHook == null || _engine == null || _keyboardOutputService == null)
            return;

        // 키보드 후크 → ExpanderEngine 연결
        _keyboardHook.OnKeyInput += (ch) =>
        {
            // UI 스레드가 아닌 곳에서 호출될 수 있으므로 Dispatcher 사용
            Dispatcher.Invoke(() =>
            {
                _engine.OnCharInput(ch);
            });
        };

        // 버퍼 리셋 이벤트 연결 (컨텍스트 변경 감지)
        _keyboardHook.OnBufferReset += () =>
        {
            Dispatcher.Invoke(() =>
            {
                _engine.ResetBuffer();
            });
        };

        // ExpanderEngine → KeyboardOutputService 연결
        // 주의: 엔진은 이벤트 발생 전에 이미 Disable 상태임 (ExpanderEngine.HandleDelimiter에서 처리)
        _engine.OnExpansionNeeded += (result) =>
        {
            // Dispatcher를 통해 UI 스레드에서 실행
            Dispatcher.Invoke(async () =>
            {
                try
                {
                    // 디버깅: 키워드와 구분자 확인
                    _logWriter?.WriteLine($"[Expansion] Keyword: '{result.Keyword}' (Length: {result.KeywordLength}), Delimiter: '{result.Delimiter}'");
                    
                    // 송신 플래그 설정 (재귀 방지)
                    _keyboardOutputService.SetSendingFlag(true);
                    _engine.SetSendingFlag(true);
                    
                    // 구분자가 화면에 찍히기를 기다림 (타이밍 이슈 해결)
                    await Task.Delay(30);

                    // 1. 키워드 + 구분자 모두 삭제 (Backspace)
                    int backspaceCount = result.KeywordLength + 1;
                    _logWriter?.WriteLine($"[Expansion] Sending {backspaceCount} backspaces");
                    _keyboardOutputService.SendBackspaces(backspaceCount);
                    
                    // 백스페이스가 완전히 처리되도록 대기
                    await Task.Delay(_currentSettings?.BackspaceDelayMs ?? 13 * 2);

                    // 2. 대체 텍스트만 입력 (구분자 없음)
                    _keyboardOutputService.SendText(result.Replacement);

                    // 3. 효과음 재생
                    _soundService?.Play();
                    
                    _logWriter?.WriteLine($"[Expansion] Completed");
                }
                finally
                {
                    // 송신 플래그 해제
                    _keyboardOutputService.SetSendingFlag(false);
                    _engine.SetSendingFlag(false);
                    
                    // 엔진 다시 활성화 전 버퍼 클리어 (확장 중 쌓인 입력 제거)
                    _engine.ResetBuffer();
                    
                    // 엔진 다시 활성화 (충분한 지연 후)
                    await Task.Delay(100);
                    _engine.Enable();
                    _logWriter?.WriteLine($"[Expansion] Engine re-enabled at {DateTime.Now:HH:mm:ss.fff}, buffer cleared");
                }
            });
        };

        // 키보드 후크 설치
        _keyboardHook.Install();
    }

    /// <summary>
    /// 메인 윈도우 표시
    /// </summary>
    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow
            {
                DataContext = _viewModel
            };
            _mainWindow.Closing += MainWindow_Closing;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
        _mainWindow.WindowState = WindowState.Normal;
    }

    /// <summary>
    /// 엔진 토글 (일시 정지/재개)
    /// </summary>
    private void ToggleEngine()
    {
        _viewModel?.ToggleEngineCommand.Execute(null);
    }

    /// <summary>
    /// 설정 창 열기
    /// </summary>
    private void OpenSettingsWindow()
    {
        if (_settingsRepository == null || _engine == null || _soundService == null)
            return;

        var settingsWindow = new SettingsWindow
        {
            Owner = _mainWindow
        };

        // 창 닫기 콜백을 전달하여 ViewModel 생성
        var settingsViewModel = new SettingsViewModel(
            _settingsRepository, 
            OnSettingsApplied,
            () => settingsWindow.Close(),  // 창 닫기 콜백
            _autoStartService  // 자동 시작 서비스
        );
        
        settingsWindow.DataContext = settingsViewModel;

        // 설정 로드
        Dispatcher.InvokeAsync(async () =>
        {
            await settingsViewModel.LoadSettingsAsync();
        });

        // 창 표시 (모달)
        settingsWindow.ShowDialog();
    }

    /// <summary>
    /// 설정이 적용되었을 때 호출
    /// </summary>
    private void OnSettingsApplied(AppSettings settings)
    {
        _currentSettings = settings;
        _engine?.SetSettings(settings);
        _keyboardOutputService?.SetSettings(settings);
        _soundService?.SetEnabled(settings.PlaySoundOnExpansion);
    }

    /// <summary>
    /// 메인 윈도우 닫기 이벤트 처리 (트레이로 최소화)
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // 창을 닫는 대신 숨김 (트레이로 최소화)
        e.Cancel = true;
        _mainWindow?.Hide();
        
        _trayIconService?.ShowBalloonTip(
            "TextExpander",
            "시스템 트레이에서 계속 실행 중입니다.",
            1500);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logWriter?.WriteLine($"=== TextExpander Stopped at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
        _logWriter?.Close();
        _logWriter?.Dispose();
        
        // 리소스 정리
        _keyboardHook?.Dispose();
        _trayIconService?.Dispose();
        _soundService?.Dispose();
        
        base.OnExit(e);
    }
}
