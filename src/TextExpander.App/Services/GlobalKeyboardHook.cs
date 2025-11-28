using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TextExpander.App.Services;

/// <summary>
/// 전역 키보드 후킹 서비스
/// Win32 SetWindowsHookEx API를 사용하여 시스템 전체 키 입력 감지
/// </summary>
public class GlobalKeyboardHook : IDisposable
{
    #region Win32 API 선언

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern int ToUnicode(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // dwExtraInfo에 설정할 마커 값 (우리가 보낸 입력 식별용)
    private static readonly IntPtr SENT_BY_US_MARKER = new IntPtr(0x54455854); // "TEXT" in hex

    #endregion

    private IntPtr _hookHandle = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookProc;
    private bool _disposed = false;
    private IntPtr _lastForegroundWindow = IntPtr.Zero;
    private System.Threading.Timer? _focusCheckTimer;
    
    // 키 중복 방지를 위한 마지막 키 입력 추적
    private uint _lastVkCode = 0;
    private uint _lastScanCode = 0;
    private uint _lastFlags = 0;
    private DateTime _lastKeyTime = DateTime.MinValue;
    private const int KEY_DEBOUNCE_MS = 100; // 100ms 내 동일 키는 무시 (더 넉넉하게)

    /// <summary>
    /// 키 입력 이벤트 (char 단위로 전달)
    /// </summary>
    public event Action<char>? OnKeyInput;

    /// <summary>
    /// 버퍼 리셋 이벤트 (컨텍스트 변경 감지)
    /// </summary>
    public event Action? OnBufferReset;

    /// <summary>
    /// 우리가 보낸 입력인지 확인하는 함수 (재귀 방지용)
    /// </summary>
    public Func<bool>? IsSendingChecker { get; set; }

    /// <summary>
    /// 키보드 후크 설치
    /// </summary>
    public void Install()
    {
        if (_hookHandle != IntPtr.Zero)
            return;

        // 델리게이트를 필드에 저장하여 GC 수집 방지
        _hookProc = HookCallback;

        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        
        if (currentModule == null)
            throw new InvalidOperationException("현재 프로세스의 메인 모듈을 가져올 수 없습니다.");

        _hookHandle = SetWindowsHookEx(
            WH_KEYBOARD_LL,
            _hookProc,
            GetModuleHandle(currentModule.ModuleName),
            0);

        if (_hookHandle == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"키보드 후크 설치 실패 (에러 코드: {errorCode})");
        }

        // 창 포커스 변경 감지 타이머 시작 (100ms 간격)
        _lastForegroundWindow = GetForegroundWindow();
        _focusCheckTimer = new System.Threading.Timer(CheckFocusChange, null, 100, 100);
    }

    /// <summary>
    /// 키보드 후크 제거
    /// </summary>
    public void Uninstall()
    {
        // 포커스 체크 타이머 정지
        _focusCheckTimer?.Dispose();
        _focusCheckTimer = null;

        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
        _hookProc = null;
    }

    /// <summary>
    /// 저수준 키보드 콜백
    /// 예외 발생 시에도 CallNextHookEx를 반드시 호출하여 시스템 안정성 보장
    /// </summary>
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                
                // KeyDown 이벤트만 처리 (KeyUp은 무시)
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    ProcessKeyDown(lParam);
                }
            }
        }
        catch
        {
            // 키보드 후크 콜백에서 예외가 발생해도 시스템에 영향을 주지 않도록 무시
            // 실제 운영 환경에서는 로깅 추가 권장
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    /// <summary>
    /// KeyDown 이벤트 처리 (예외 발생 가능 로직 분리)
    /// </summary>
    private void ProcessKeyDown(IntPtr lParam)
    {
        var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

        // 우리가 보낸 입력인지 확인 (dwExtraInfo 마커 체크)
        if (hookStruct.dwExtraInfo == SENT_BY_US_MARKER)
            return;

        // IsSending 플래그 체크 (추가 안전장치)
        if (IsSendingChecker?.Invoke() == true)
            return;

        uint vkCode = hookStruct.vkCode;
        uint scanCode = hookStruct.scanCode;
        uint flags = hookStruct.flags;
        uint time = hookStruct.time;
        
        // 키 중복 방지: 동일한 키 이벤트가 짧은 시간 내에 여러 번 전달될 수 있음
        var now = DateTime.UtcNow;
        if (vkCode == _lastVkCode && scanCode == _lastScanCode && flags == _lastFlags)
        {
            var elapsed = (now - _lastKeyTime).TotalMilliseconds;
            if (elapsed < KEY_DEBOUNCE_MS)
            {
                return; // 중복 키 입력 무시
            }
        }
        
        _lastVkCode = vkCode;
        _lastScanCode = scanCode;
        _lastFlags = flags;
        _lastKeyTime = now;
        
        // 키보드 상태 가져오기 (Ctrl, Alt 등 조합키 확인용)
        var keyState = new byte[256];
        GetKeyboardState(keyState);
        bool isCtrlPressed = (keyState[0x11] & 0x80) != 0; // VK_CONTROL
        bool isAltPressed = (keyState[0x12] & 0x80) != 0;  // VK_MENU

        // 1. 버퍼 리셋이 필요한 키들 (컨텍스트 변경)
        if (ShouldResetBuffer(vkCode, isCtrlPressed, isAltPressed))
        {
            OnBufferReset?.Invoke();
            return;
        }

        // 2. 일반 문자 입력 처리
        ProcessCharacterInput(vkCode, hookStruct.scanCode);
    }

    /// <summary>
    /// 버퍼 리셋이 필요한 키인지 판별
    /// </summary>
    private bool ShouldResetBuffer(uint vkCode, bool isCtrlPressed, bool isAltPressed)
    {
        // 방향키
        if (vkCode >= 0x25 && vkCode <= 0x28) // VK_LEFT, VK_UP, VK_RIGHT, VK_DOWN
            return true;

        // 탐색 키
        if (vkCode == 0x21 || vkCode == 0x22) // VK_PRIOR (Page Up), VK_NEXT (Page Down)
            return true;
        if (vkCode == 0x24 || vkCode == 0x23) // VK_HOME, VK_END
            return true;

        // Delete 키
        if (vkCode == 0x2E) // VK_DELETE
            return true;

        // Ctrl 조합 단축키
        if (isCtrlPressed)
        {
            switch (vkCode)
            {
                case 0x43: // C (복사)
                case 0x58: // X (잘라내기)
                case 0x56: // V (붙여넣기)
                case 0x5A: // Z (실행 취소)
                case 0x59: // Y (다시 실행)
                    return true;
            }
        }

        // Alt+Tab (애플리케이션 전환)
        if (isAltPressed && vkCode == 0x09) // VK_TAB
            return true;

        return false;
    }

    /// <summary>
    /// 일반 문자 입력 처리
    /// </summary>
    private void ProcessCharacterInput(uint vkCode, uint scanCode)
    {
        // Backspace (버퍼 리셋으로 처리되지만 ExpanderEngine에서 별도 처리)
        if (vkCode == 0x08) // VK_BACK
        {
            OnKeyInput?.Invoke('\b');
        }
        // Enter
        else if (vkCode == 0x0D) // VK_RETURN
        {
            OnKeyInput?.Invoke('\n');
        }
        // Tab
        else if (vkCode == 0x09) // VK_TAB
        {
            OnKeyInput?.Invoke('\t');
        }
        // Space
        else if (vkCode == 0x20) // VK_SPACE
        {
            OnKeyInput?.Invoke(' ');
        }
        // 일반 문자 키
        else
        {
            char? ch = VirtualKeyToChar(vkCode, scanCode);
            if (ch.HasValue && !char.IsControl(ch.Value))
            {
                OnKeyInput?.Invoke(ch.Value);
            }
        }
    }

    /// <summary>
    /// 가상 키 코드를 유니코드 문자로 변환
    /// </summary>
    private static char? VirtualKeyToChar(uint vkCode, uint scanCode)
    {
        var keyboardState = new byte[256];
        if (!GetKeyboardState(keyboardState))
            return null;

        var buffer = new System.Text.StringBuilder(2);
        int result = ToUnicode(vkCode, scanCode, keyboardState, buffer, buffer.Capacity, 0);

        if (result == 1)
        {
            return buffer[0];
        }
        else if (result == 2)
        {
            // 데드 키 (악센트 등) - 첫 번째 문자 반환
            return buffer[0];
        }

        return null;
    }

    /// <summary>
    /// 창 포커스 변경 감지 (타이머 콜백)
    /// </summary>
    private void CheckFocusChange(object? state)
    {
        try
        {
            IntPtr currentWindow = GetForegroundWindow();
            
            // 포커스가 변경되었고, 유효한 창이면 버퍼 리셋
            if (currentWindow != IntPtr.Zero && 
                currentWindow != _lastForegroundWindow &&
                _lastForegroundWindow != IntPtr.Zero)
            {
                OnBufferReset?.Invoke();
            }
            
            _lastForegroundWindow = currentWindow;
        }
        catch
        {
            // 타이머 콜백에서 예외 발생 시 무시
        }
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Uninstall();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~GlobalKeyboardHook()
    {
        Dispose();
    }
}

