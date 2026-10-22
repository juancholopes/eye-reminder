using EyeReminder.Interop;
using EyeReminder.ViewModels;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace EyeReminder.Views;

public sealed partial class MainWindow : Window
{
    // Tamaño mínimo (en píxeles independientes del DPI) con el que el contenido se ve completo.
    // Alto: título + tarjeta del contador + botones + el botón extra "Omitir este descanso".
    private const int MinWidth = 400;
    private const int MinHeight = 460;

    // Id distinto al del subclase de TrayIcon, que usa la misma ventana. El campo evita que el GC libere el delegado.
    private static readonly UIntPtr MinSizeSubclassId = new(1);
    private readonly NativeMethods.SubclassProc _minSizeProc;

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var hwnd = WindowNative.GetWindowHandle(this);
        _minSizeProc = MinSizeProc;
        NativeMethods.SetWindowSubclass(hwnd, _minSizeProc, MinSizeSubclassId, UIntPtr.Zero);

        var scale = NativeMethods.GetDpiForWindow(hwnd) / 96.0;
        AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(440 * scale), (int)(520 * scale)));

        Closed += (_, _) => NativeMethods.RemoveWindowSubclass(hwnd, _minSizeProc, MinSizeSubclassId);
    }

    public MainViewModel ViewModel { get; }

    private IntPtr MinSizeProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr refData)
    {
        if (message == NativeMethods.WM_GETMINMAXINFO)
        {
            var scale = NativeMethods.GetDpiForWindow(hwnd) / 96.0;
            var info = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.MINMAXINFO>(lParam);
            info.ptMinTrackSize.X = (int)(MinWidth * scale);
            info.ptMinTrackSize.Y = (int)(MinHeight * scale);
            System.Runtime.InteropServices.Marshal.StructureToPtr(info, lParam, false);
            return IntPtr.Zero;
        }

        return NativeMethods.DefSubclassProc(hwnd, message, wParam, lParam);
    }
}
