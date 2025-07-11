using Epic7SecretShopAutoBuyer;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;


public static class MouseClickDetector
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;

    private static IntPtr _hookID = IntPtr.Zero;
    private static LowLevelMouseProc _proc = HookCallback;
    private static NextMove _nextMove;

    public static void Start(NextMove nextMove)
    {
        if (_hookID != IntPtr.Zero)
            return; // Already hooked

        _nextMove = nextMove;
        _hookID = SetHook(_proc);
        Console.WriteLine("Listening for physical left clicks...");

        Application.Run(); // Keep the hook alive
    }

    public static void Stop()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
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
            _nextMove?.Invoke();
            Console.WriteLine($"Physical mouse click detected at {DateTime.Now}");
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    #region Windows API
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
