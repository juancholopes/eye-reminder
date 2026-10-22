using System.Runtime.InteropServices;

namespace EyeReminder.Interop;

/// <summary>Win32 para que una ventana de WinUI tenga transparencia por píxel.</summary>
internal static partial class NativeMethods
{
    internal const int DWM_BB_ENABLE = 0x1;
    internal const int DWM_BB_BLURREGION = 0x2;

    internal const int DQTYPE_THREAD_CURRENT = 2;
    internal const int DQTAT_COM_STA = 2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct DWM_BLURBEHIND
    {
        public int dwFlags;
        public int fEnable;
        public IntPtr hRgnBlur;
        public int fTransitionOnMaximized;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DispatcherQueueOptions
    {
        public int dwSize;
        public int threadType;
        public int apartmentType;
    }

    [DllImport("dwmapi.dll")]
    internal static extern int DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

    [DllImport("CoreMessaging.dll")]
    internal static extern int CreateDispatcherQueueController(DispatcherQueueOptions options, out IntPtr controller);
}
