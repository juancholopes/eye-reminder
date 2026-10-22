using System.Runtime.InteropServices;

namespace EyeReminder.Interop;

internal static partial class NativeMethods
{
    internal const int GWL_STYLE = -16;
    internal const int GWL_EXSTYLE = -20;
    internal const long WS_POPUP = 0x80000000L;
    internal const long WS_CAPTION = 0x00C00000L;
    internal const long WS_THICKFRAME = 0x00040000L;
    internal const long WS_SYSMENU = 0x00080000L;
    internal const long WS_MINIMIZEBOX = 0x00020000L;
    internal const long WS_MAXIMIZEBOX = 0x00010000L;
    internal const long WS_EX_NOACTIVATE = 0x08000000L;
    internal const long WS_EX_TOOLWINDOW = 0x00000080L;
    internal const long WS_EX_TOPMOST = 0x00000008L;
    internal const long WS_EX_WINDOWEDGE = 0x00000100L;

    internal const uint WM_ERASEBKGND = 0x0014;
    internal const uint WM_STYLECHANGING = 0x007C;
    internal const uint WM_NCCALCSIZE = 0x0083;

    [StructLayout(LayoutKind.Sequential)]
    internal struct STYLESTRUCT
    {
        public uint styleOld;
        public uint styleNew;
    }

    internal static readonly IntPtr HWND_TOPMOST = new(-1);
    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_FRAMECHANGED = 0x0020;

    internal const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    internal const int DWMWCP_DONOTROUND = 1;
    internal const int DWMWA_BORDER_COLOR = 34;
    internal const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

    internal const int RGN_OR = 2;
    internal const int RGN_DIFF = 4;

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    internal static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    internal static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    internal static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    internal static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [DllImport("gdi32.dll")]
    internal static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("gdi32.dll")]
    internal static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

    [DllImport("gdi32.dll")]
    internal static extern IntPtr CreateEllipticRgn(int left, int top, int right, int bottom);

    [DllImport("gdi32.dll")]
    internal static extern int CombineRgn(IntPtr destination, IntPtr source1, IntPtr source2, int mode);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(IntPtr handle);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
