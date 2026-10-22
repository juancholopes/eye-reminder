using EyeReminder.Interop;

namespace EyeReminder.Services;

/// <summary>
/// Recibe WM_WTSSESSION_CHANGE subclasificando una ventana (la principal, que sigue existiendo
/// aunque esté oculta). El mensaje llega por el hilo de la interfaz, así que los eventos también.
/// </summary>
public sealed class SessionLockService : ISessionLockService, IDisposable
{
    private readonly IntPtr _hwnd;

    // Se guarda en un campo para que el GC no libere el delegado mientras Win32 lo usa.
    private readonly NativeMethods.SubclassProc _subclassProc;
    private readonly UIntPtr _subclassId = new(2);

    private bool _disposed;

    public SessionLockService(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _subclassProc = WindowProc;
        NativeMethods.SetWindowSubclass(_hwnd, _subclassProc, _subclassId, UIntPtr.Zero);
        NativeMethods.WTSRegisterSessionNotification(_hwnd, NativeMethods.NOTIFY_FOR_THIS_SESSION);
    }

    public event EventHandler? Locked;

    public event EventHandler? Unlocked;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        NativeMethods.WTSUnRegisterSessionNotification(_hwnd);
        NativeMethods.RemoveWindowSubclass(_hwnd, _subclassProc, _subclassId);
    }

    private IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr refData)
    {
        if (message == NativeMethods.WM_WTSSESSION_CHANGE)
        {
            switch ((int)wParam.ToInt64())
            {
                case NativeMethods.WTS_SESSION_LOCK:
                    Locked?.Invoke(this, EventArgs.Empty);
                    break;
                case NativeMethods.WTS_SESSION_UNLOCK:
                    Unlocked?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        return NativeMethods.DefSubclassProc(hwnd, message, wParam, lParam);
    }
}
