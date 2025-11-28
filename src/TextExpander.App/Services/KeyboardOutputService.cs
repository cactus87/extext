using System.Runtime.InteropServices;
using TextExpander.Core.Models;

namespace TextExpander.App.Services;

/// <summary>
/// 키보드 출력 서비스
/// Win32 SendInput API를 사용하여 키 입력 시뮬레이션
/// </summary>
public class KeyboardOutputService : IDisposable
{
    #region Win32 API 선언

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    private const ushort VK_BACK = 0x08;
    private const ushort VK_RETURN = 0x0D;
    private const ushort VK_TAB = 0x09;
    private const ushort VK_SPACE = 0x20;

    // 우리가 보낸 입력임을 식별하기 위한 마커
    // "TEXT" (0x54=T, 0x45=E, 0x58=X, 0x54=T in ASCII hex)
    private static readonly IntPtr SENT_BY_US_MARKER = new IntPtr(0x54455854);

    #endregion

    private volatile bool _isSending = false;
    private readonly object _sendLock = new();
    private AppSettings _settings = new();

    /// <summary>
    /// 현재 키 송신 중인지 여부 (스레드 안전)
    /// </summary>
    public bool IsSending => _isSending;

    /// <summary>
    /// 송신 플래그 설정 (스레드 안전)
    /// </summary>
    public void SetSendingFlag(bool sending)
    {
        _isSending = sending;
    }

    /// <summary>
    /// 설정 업데이트
    /// </summary>
    public void SetSettings(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Backspace 키를 지정된 횟수만큼 전송
    /// </summary>
    public void SendBackspaces(int count)
    {
        if (count <= 0) return;

        lock (_sendLock)
        {
            // 메모장 등 느린 애플리케이션 호환성을 위해 한 번에 하나씩 전송
            for (int i = 0; i < count; i++)
            {
                var inputs = new INPUT[]
                {
                    CreateKeyInput(VK_BACK, false),
                    CreateKeyInput(VK_BACK, true)
                };
                
                uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
                
                // 각 키 사이 딜레이 (메모장 호환성)
                // 딜레이 전에 백스페이스가 처리되도록 충분한 시간 확보
                Thread.Sleep(_settings.BackspaceDelayMs);
            }
            
            // 모든 백스페이스 전송 후 추가 딜레이 (마지막 백스페이스가 처리되도록)
            Thread.Sleep(_settings.BackspaceDelayMs);
        }
    }

    /// <summary>
    /// 텍스트 문자열 전송 (Unicode 방식)
    /// </summary>
    public void SendText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        lock (_sendLock)
        {
            foreach (char ch in text)
            {
                INPUT[] inputs;
                
                // 줄바꿈 처리
                if (ch == '\n')
                {
                    inputs = new[]
                    {
                        CreateKeyInput(VK_RETURN, false),
                        CreateKeyInput(VK_RETURN, true)
                    };
                }
                else if (ch == '\r')
                {
                    // CR은 무시 (Windows에서 줄바꿈은 보통 CRLF지만 Enter 한 번으로 처리)
                    continue;
                }
                else if (ch == '\t')
                {
                    inputs = new[]
                    {
                        CreateKeyInput(VK_TAB, false),
                        CreateKeyInput(VK_TAB, true)
                    };
                }
                else
                {
                    // Unicode 문자로 전송
                    inputs = new[]
                    {
                        CreateUnicodeInput(ch, false),
                        CreateUnicodeInput(ch, true)
                    };
                }

                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
                
                // 각 문자 사이 딜레이 (메모장 호환성)
                Thread.Sleep(_settings.TextCharDelayMs);
            }
        }
    }

    /// <summary>
    /// 단일 문자 전송
    /// </summary>
    public void SendChar(char ch)
    {
        lock (_sendLock)
        {
            INPUT[] inputs;

            switch (ch)
            {
                case '\n':
                    inputs = new[]
                    {
                        CreateKeyInput(VK_RETURN, false),
                        CreateKeyInput(VK_RETURN, true)
                    };
                    break;
                case '\r':
                    // CR은 무시
                    return;
                case '\t':
                    inputs = new[]
                    {
                        CreateKeyInput(VK_TAB, false),
                        CreateKeyInput(VK_TAB, true)
                    };
                    break;
                case ' ':
                    inputs = new[]
                    {
                        CreateKeyInput(VK_SPACE, false),
                        CreateKeyInput(VK_SPACE, true)
                    };
                    break;
                default:
                    inputs = new[]
                    {
                        CreateUnicodeInput(ch, false),
                        CreateUnicodeInput(ch, true)
                    };
                    break;
            }

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            
            // 메모장 호환성을 위한 딜레이 추가
            Thread.Sleep(_settings.TextCharDelayMs);
        }
    }

    /// <summary>
    /// 가상 키 코드 기반 키 입력 생성
    /// </summary>
    private static INPUT CreateKeyInput(ushort vkCode, bool keyUp)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            union = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = vkCode,
                    wScan = 0,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = SENT_BY_US_MARKER
                }
            }
        };
    }

    /// <summary>
    /// Unicode 문자 기반 키 입력 생성
    /// </summary>
    private static INPUT CreateUnicodeInput(char ch, bool keyUp)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            union = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = KEYEVENTF_UNICODE | (keyUp ? KEYEVENTF_KEYUP : 0),
                    time = 0,
                    dwExtraInfo = SENT_BY_US_MARKER
                }
            }
        };
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        // 현재는 관리되지 않는 리소스가 없지만,
        // 향후 SemaphoreSlim 등 IDisposable 리소스 추가 시 여기서 정리
        GC.SuppressFinalize(this);
    }
}

