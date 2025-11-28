using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace TextExpander.App.Services;

/// <summary>
/// Windows 시작 프로그램 등록 서비스
/// </summary>
public class AutoStartService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TextExpander";

    /// <summary>
    /// 자동 시작이 활성화되어 있는지 확인
    /// </summary>
    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 자동 시작 활성화
    /// </summary>
    public void EnableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key != null)
            {
                // 현재 실행 중인 EXE 경로 가져오기
                string exePath = Process.GetCurrentProcess().MainModule?.FileName 
                    ?? GetExecutablePath();
                
                // 전체 경로로 설정
                key.SetValue(AppName, $"\"{exePath}\"");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"자동 시작 등록 실패: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 자동 시작 비활성화
    /// </summary>
    public void DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.DeleteValue(AppName, false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"자동 시작 해제 실패: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 실행 파일 경로 가져오기 (Single-file 앱 지원)
    /// </summary>
    private static string GetExecutablePath()
    {
        // Single-file 앱의 경우 AppContext.BaseDirectory 사용
        if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
        {
            var exeName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]) + ".exe";
            return Path.Combine(AppContext.BaseDirectory, exeName);
        }
        
        // 일반 앱의 경우 Assembly.Location 사용
        return System.Reflection.Assembly.GetExecutingAssembly().Location;
    }
}
