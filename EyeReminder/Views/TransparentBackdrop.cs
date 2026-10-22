using EyeReminder.Interop;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace EyeReminder.Views;

/// <summary>
/// Fondo de sistema totalmente transparente: deja ver lo que hay detrás de la ventana para que
/// la forma del notch la dibuje XAML con bordes suavizados (antialiasing).
/// </summary>
internal sealed class TransparentBackdrop : SystemBackdrop
{
    private static Windows.UI.Composition.Compositor? s_compositor;
    private static IntPtr s_dispatcherQueueController;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        connectedTarget.SystemBackdrop = GetCompositor().CreateColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        disconnectedTarget.SystemBackdrop = null;
        base.OnTargetDisconnected(disconnectedTarget);
    }

    private static Windows.UI.Composition.Compositor GetCompositor()
    {
        if (s_compositor is not null)
        {
            return s_compositor;
        }

        // El compositor del sistema (Windows.UI.Composition) exige una DispatcherQueue de
        // Windows.System en el hilo; el hilo de WinUI 3 solo trae la de Microsoft.UI.
        if (Windows.System.DispatcherQueue.GetForCurrentThread() is null)
        {
            var options = new NativeMethods.DispatcherQueueOptions
            {
                dwSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.DispatcherQueueOptions>(),
                threadType = NativeMethods.DQTYPE_THREAD_CURRENT,
                apartmentType = NativeMethods.DQTAT_COM_STA,
            };
            NativeMethods.CreateDispatcherQueueController(options, out s_dispatcherQueueController);
        }

        s_compositor = new Windows.UI.Composition.Compositor();
        return s_compositor;
    }
}
