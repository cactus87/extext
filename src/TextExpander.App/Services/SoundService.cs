using System.IO;
using System.Media;
using System.Reflection;

namespace TextExpander.App.Services;

/// <summary>
/// 효과음 재생 서비스
/// 스니펫 확장 시 알림음 재생
/// </summary>
public class SoundService
{
    private readonly SoundPlayer _soundPlayer;
    private bool _isEnabled = false;

    public SoundService()
    {
        // 시스템 기본 알림음 사용
        _soundPlayer = new SoundPlayer();
        try
        {
            // Windows 기본 알림음 경로
            var soundPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Media", "Windows Notify.wav"
            );

            if (File.Exists(soundPath))
            {
                _soundPlayer.SoundLocation = soundPath;
                _soundPlayer.LoadAsync();
            }
            else
            {
                // 대체: 시스템 비프음 사용
                _soundPlayer.Stream = null;
            }
        }
        catch
        {
            // 오류 발생 시 시스템 비프음으로 대체
            _soundPlayer.Stream = null;
        }
    }

    /// <summary>
    /// 효과음 활성화/비활성화 설정
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
    }

    /// <summary>
    /// 효과음 재생
    /// </summary>
    public void Play()
    {
        if (!_isEnabled) return;

        try
        {
            if (_soundPlayer.Stream != null || !string.IsNullOrEmpty(_soundPlayer.SoundLocation))
            {
                // 비동기로 재생 (UI 블로킹 방지)
                _soundPlayer.Play();
            }
            else
            {
                // 시스템 비프음
                System.Console.Beep(800, 100);
            }
        }
        catch
        {
            // 재생 실패 시 무시 (앱이 중단되지 않도록)
        }
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        _soundPlayer?.Dispose();
    }
}

