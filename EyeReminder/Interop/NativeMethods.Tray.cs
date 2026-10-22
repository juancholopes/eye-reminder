using System.Runtime.InteropServices;

namespace EyeReminder.Interop;

/// <summary>Win32 para el icono de la bandeja del sistema y su menú contextual.</summary>
internal static partial class NativeMethods
{
    internal const int NIM_ADD = 0x0;
    internal const int NIM_MODIFY = 0x1;
    internal const int NIM_DELETE = 0x2;
    internal const int NIM_SETVERSION = 0x4;
    internal const int NOTIFYICON_VERSION_4 = 4;

    internal const int NIF_MESSAGE = 0x01;
    internal const int NIF_ICON = 0x02;
    internal const int NIF_TIP = 0x04;
    internal const int NIF_INFO = 0x10;
    internal const int NIF_SHOWTIP = 0x80;
    internal const int NIIF_USER = 0x04;
    internal const int NIIF_RESPECT_QUIET_TIME = 0x80;

    internal const uint WM_NULL = 0x0000;
    internal const uint WM_CONTEXTMENU = 0x007B;
    internal const uint WM_LBUTTONDBLCLK = 0x0203;
    internal const uint WM_APP = 0x8000;
    internal const uint NIN_SELECT = 0x0400;
    internal const uint NIN_KEYSELECT = 0x0401;

    internal const uint MF_STRING = 0x0000;
    internal const uint MF_GRAYED = 0x0001;
    internal const uint MF_SEPARATOR = 0x0800;
    internal const uint TPM_RIGHTBUTTON = 0x0002;
    internal const uint TPM_RETURNCMD = 0x0100;
    internal const uint TPM_BOTTOMALIGN = 0x0020;

    internal const uint IMAGE_ICON = 1;
    internal const uint LR_LOADFROMFILE = 0x10;
    internal const int SM_CXSMICON = 49;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NOTIFYICONDATAW
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    internal delegate IntPtr SubclassProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr refData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Shell_NotifyIcon(int message, ref NOTIFYICONDATAW data);

    [DllImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowSubclass(IntPtr hwnd, SubclassProc proc, UIntPtr id, UIntPtr refData);

    [DllImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RemoveWindowSubclass(IntPtr hwnd, SubclassProc proc, UIntPtr id);

    [DllImport("comctl32.dll")]
    internal static extern IntPtr DefSubclassProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint RegisterWindowMessage(string name);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern IntPtr LoadImage(IntPtr instance, string name, uint type, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr icon);

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    internal static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AppendMenu(IntPtr menu, uint flags, UIntPtr id, string? text);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetMenuDefaultItem(IntPtr menu, uint item, [MarshalAs(UnmanagedType.Bool)] bool byPosition);

    [DllImport("user32.dll")]
    internal static extern uint TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hwnd, IntPtr parameters);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyMenu(IntPtr menu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
}
