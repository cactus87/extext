using System.Drawing;
using System.IO;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace TextExpander.App.Services;

/// <summary>
/// 시스템 트레이 아이콘 관리 서비스
/// </summary>
public class TrayIconService : IDisposable
{
    private WinForms.NotifyIcon? _notifyIcon;
    private readonly Action _onShowWindow;
    private readonly Action _onTogglePause;
    private readonly Action _onExit;

    public TrayIconService(Action onShowWindow, Action onTogglePause, Action onExit)
    {
        _onShowWindow = onShowWindow ?? throw new ArgumentNullException(nameof(onShowWindow));
        _onTogglePause = onTogglePause ?? throw new ArgumentNullException(nameof(onTogglePause));
        _onExit = onExit ?? throw new ArgumentNullException(nameof(onExit));
    }

    /// <summary>
    /// 트레이 아이콘 초기화
    /// </summary>
    public void Initialize()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application,
            Visible = true,
            Text = "TextExpander"
        };

        // 더블클릭 시 메인 윈도우 표시
        _notifyIcon.DoubleClick += (s, e) => _onShowWindow();

        // 우클릭 컨텍스트 메뉴
        var contextMenu = new WinForms.ContextMenuStrip();
        
        contextMenu.Items.Add("열기", null, (s, e) => _onShowWindow());
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add("일시 정지/재개", null, (s, e) => _onTogglePause());
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add("종료", null, (s, e) => _onExit());

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    /// <summary>
    /// 풍선 팁 표시
    /// </summary>
    public void ShowBalloonTip(string title, string text, int duration = 2000)
    {
        _notifyIcon?.ShowBalloonTip(duration, title, text, WinForms.ToolTipIcon.Info);
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}

