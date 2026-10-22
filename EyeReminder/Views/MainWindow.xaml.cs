using EyeReminder.Interop;
using EyeReminder.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace EyeReminder.Views;

public sealed partial class MainWindow : Window
{
    // Ancho mínimo (en píxeles independientes del DPI) con el que el contenido se ve completo.
    private const int MinWidth = 400;

    // Ancho de diseño del contenido (ver ContentPanel en el XAML); el Viewbox lo escala a partir de este ancho.
    private const double DesignWidth = 440;

    // Escala máxima del contenido; debe coincidir con el MaxWidth del Viewbox (DesignWidth x MaxZoom).
    private const double MaxZoom = 1.5;

    // Alto del contenido (tarjeta + botones) a su escala natural, sin la barra de título.
    // Cambia al aparecer el botón "Omitir este descanso", por eso se recalcula en FitHeightToContent.
    private double _contentHeight = 340;

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
        AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(DesignWidth * scale), (int)((AppTitleBar.Height + _contentHeight) * scale)));

        // Con el layout ya hecho se mide el contenido real y se ajusta el alto de la ventana.
        ContentPanel.Loaded += (_, _) => FitHeightToContent();
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsOnBreak))
                DispatcherQueue.TryEnqueue(FitHeightToContent);
        };

        Closed += (_, _) => NativeMethods.RemoveWindowSubclass(hwnd, _minSizeProc, MinSizeSubclassId);
    }

    public MainViewModel ViewModel { get; }

    private void FitHeightToContent()
    {
        ContentPanel.Measure(new Windows.Foundation.Size(DesignWidth, double.PositiveInfinity));
        _contentHeight = ContentPanel.DesiredSize.Height;

        // Maximizada o en pantalla completa el tamaño no es nuestro: el Viewbox ya escala y centra el contenido.
        if (AppWindow.Presenter is not OverlappedPresenter { State: OverlappedPresenterState.Restored })
            return;

        var hwnd = WindowNative.GetWindowHandle(this);
        var scale = NativeMethods.GetDpiForWindow(hwnd) / 96.0;

        // Se conserva la proporción: el alto deseado sale del ancho actual y del factor con que el Viewbox escala.
        var zoom = Math.Min(AppWindow.ClientSize.Width / scale / DesignWidth, MaxZoom);
        var targetHeight = (int)Math.Ceiling((AppTitleBar.Height + _contentHeight * zoom) * scale);

        // Resize recibe el tamaño exterior: se suma la diferencia entre el alto de cliente deseado y el actual.
        var delta = targetHeight - AppWindow.ClientSize.Height;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(AppWindow.Size.Width, AppWindow.Size.Height + delta));
    }

    private IntPtr MinSizeProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr refData)
    {
        if (message == NativeMethods.WM_GETMINMAXINFO)
        {
            var scale = NativeMethods.GetDpiForWindow(hwnd) / 96.0;
            var info = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.MINMAXINFO>(lParam);
            info.ptMinTrackSize.X = (int)(MinWidth * scale);
            info.ptMinTrackSize.Y = (int)((AppTitleBar.Height + _contentHeight * MinWidth / DesignWidth) * scale)
                                    + (AppWindow.Size.Height - AppWindow.ClientSize.Height);
            System.Runtime.InteropServices.Marshal.StructureToPtr(info, lParam, false);
            return IntPtr.Zero;
        }

        return NativeMethods.DefSubclassProc(hwnd, message, wParam, lParam);
    }
}
