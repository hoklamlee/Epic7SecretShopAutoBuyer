using Epic7SecretShopAutoBuyer;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;


public static class ClickInWindowDetector
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;

    private static IntPtr _hookID = IntPtr.Zero;
    private static LowLevelMouseProc _proc = HookCallback;
    private static NextMove _nextMove;

    private static string _targetProcessName = "EpicSeven";

    public static void Start(NextMove nextMove, string? targetProcessName = null)
    {
        _nextMove = nextMove;
        if (!string.IsNullOrWhiteSpace(targetProcessName))
            _targetProcessName = targetProcessName;

        _hookID = SetHook(_proc);
        Console.WriteLine("Listening for physical left clicks...");
        Application.Run(); // Keeps the app alive
    }

    private static IntPtr SetHook(LowLevelMouseProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WH_MOUSE_LL, proc,
            GetModuleHandle(curModule.ModuleName), 0);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
        {
            GetCursorPos(out POINT pt);

            IntPtr hWnd = WindowFromPoint(pt);
            GetWindowThreadProcessId(hWnd, out uint pid);

            try
            {
                var clickedProc = Process.GetProcessById((int)pid);
                if (!clickedProc.ProcessName.Contains(_targetProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"✅ Real click inside '{_targetProcessName}' at {pt.X},{pt.Y}");
                    _nextMove?.Invoke();
                }
            }
            catch { }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    #region WinAPI

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk,
        int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    #endregion
}
