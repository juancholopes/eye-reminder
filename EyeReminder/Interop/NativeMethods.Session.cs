using System.Runtime.InteropServices;

namespace EyeReminder.Interop;

/// <summary>Win32 para enterarse de cuándo se bloquea o desbloquea la sesión de Windows.</summary>
internal static partial class NativeMethods
{
    internal const uint WM_WTSSESSION_CHANGE = 0x02B1;
    internal const int WTS_SESSION_LOCK = 0x7;
    internal const int WTS_SESSION_UNLOCK = 0x8;

    // Solo interesan los eventos de la sesión en la que corre la app.
    internal const int NOTIFY_FOR_THIS_SESSION = 0;

    [DllImport("wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSRegisterSessionNotification(IntPtr hwnd, int flags);

    [DllImport("wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSUnRegisterSessionNotification(IntPtr hwnd);
}
