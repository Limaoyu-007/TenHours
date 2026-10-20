using System.Runtime.InteropServices;

namespace TenHours;

internal static class NativeMethods
{
    internal const int WM_NCLBUTTONDOWN = 0xA1;
    internal const int WM_POWERBROADCAST = 0x218;
    internal const int WM_WTSSESSION_CHANGE = 0x2B1;
    internal const int WM_TIMECHANGE = 0x1E;
    internal const int WM_QUERYENDSESSION = 0x11;
    internal const int WM_ENDSESSION = 0x16;
    internal static readonly uint ShowMessage = RegisterWindowMessage("TenHours.ShowWindow.v1");

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(nint hwnd, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")]
    internal static extern nint SendMessage(nint hwnd, int message, nint wParam, nint lParam);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    internal static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("gdi32.dll")]
    internal static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int width, int height);
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(nint handle);
    [DllImport("Wtsapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSRegisterSessionNotification(nint hwnd, int flags);
    [DllImport("Wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSUnRegisterSessionNotification(nint hwnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(nint icon);
}
