using System.ComponentModel;
using EyeReminder.Interop;
using EyeReminder.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using WinRT.Interop;

namespace EyeReminder.Views;

/// <summary>
/// Notch sin bordes ni foco, pegado al borde superior de la pantalla principal y por encima
/// de todas las ventanas. Se muestra y oculta según <see cref="IslandViewModel.IsOpen"/>.
/// </summary>
public sealed partial class IslandWindow : Window
{
    // Medidas en píxeles independientes del dispositivo; deben coincidir con el Path del XAML.
    private const int NotchWidth = 400;
    private const int NotchHeight = 60;

    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(260);

    private readonly IntPtr _hwnd;
    private readonly DispatcherQueueTimer _topmostTimer;

    // Se guarda en un campo para que el GC no libere el delegado mientras Win32 lo usa.
    private readonly NativeMethods.SubclassProc _subclassProc;
    private Storyboard? _runningStoryboard;

    public IslandWindow(IslandViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);
        _subclassProc = WindowProc;
        NativeMethods.SetWindowSubclass(_hwnd, _subclassProc, UIntPtr.Zero, UIntPtr.Zero);

        // Otras apps también usan "siempre visible"; se reafirma mientras el notch está abierto.
        _topmostTimer = DispatcherQueue.CreateTimer();
        _topmostTimer.Interval = TimeSpan.FromMilliseconds(500);
        _topmostTimer.Tick += (_, _) => BringToTop();

        ConfigureWindow();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public IslandViewModel ViewModel { get; }

    private void ConfigureWindow()
    {
        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        AppWindow.SetPresenter(presenter);
        AppWindow.IsShownInSwitchers = false;
        ApplyPopupStyles();

        // Transparencia por píxel: DWM compone el contenido con su canal alfa en lugar de
        // pintar un fondo opaco. La región vacía evita que se aplique un desenfoque real.
        var emptyRegion = NativeMethods.CreateRectRgn(-2, -2, -1, -1);
        var blurBehind = new NativeMethods.DWM_BLURBEHIND
        {
            dwFlags = NativeMethods.DWM_BB_ENABLE | NativeMethods.DWM_BB_BLURREGION,
            fEnable = 1,
            hRgnBlur = emptyRegion,
        };
        NativeMethods.DwmEnableBlurBehindWindow(_hwnd, ref blurBehind);
        NativeMethods.DeleteObject(emptyRegion);

        // Sin esquinas ni borde de DWM: la forma la dibuja el Path del XAML.
        var corner = NativeMethods.DWMWCP_DONOTROUND;
        NativeMethods.DwmSetWindowAttribute(_hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
        var borderColor = NativeMethods.DWMWA_COLOR_NONE;
        NativeMethods.DwmSetWindowAttribute(_hwnd, NativeMethods.DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));

        // WinUI no dibuja el contenido de una ventana que nunca se activó, así que se activa
        // una vez fuera de pantalla al arrancar y se oculta; luego basta con Show(false).
        AppWindow.Move(new PointInt32(-32000, -32000));
        Activate();
        AppWindow.Hide();
    }

    /// <summary>Deja la ventana como popup pura; <see cref="WindowProc"/> impide que AppWindow lo revierta.</summary>
    private void ApplyPopupStyles()
    {
        var style = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE).ToInt64();
        NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_STYLE, new IntPtr(ToPopupStyle(style)));

        var exStyle = NativeMethods.GetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        NativeMethods.SetWindowLongPtr(_hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(ToPopupExStyle(exStyle)));

        NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);
    }

    private static long ToPopupStyle(long style) =>
        (style & ~(NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME | NativeMethods.WS_SYSMENU
                   | NativeMethods.WS_MINIMIZEBOX | NativeMethods.WS_MAXIMIZEBOX))
        | NativeMethods.WS_POPUP;

    // Fuera de la barra de tareas, sin robar el foco al hacer clic y siempre visible.
    private static long ToPopupExStyle(long exStyle) =>
        (exStyle & ~NativeMethods.WS_EX_WINDOWEDGE)
        | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST;

    private unsafe IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr refData)
    {
        switch (message)
        {
            // AppWindow vuelve a poner WS_DLGFRAME y WS_EX_WINDOWEDGE (el marco blanco de 1 px)
            // al mostrarse; se corrigen aquí antes de que se apliquen.
            case NativeMethods.WM_STYLECHANGING:
                var styles = (NativeMethods.STYLESTRUCT*)lParam;
                styles->styleNew = wParam.ToInt32() == NativeMethods.GWL_STYLE
                    ? (uint)ToPopupStyle(styles->styleNew)
                    : (uint)ToPopupExStyle(styles->styleNew);
                break;

            // Sin área no cliente: toda la ventana es contenido de XAML, sin borde de ningún tipo.
            case NativeMethods.WM_NCCALCSIZE when wParam != IntPtr.Zero:
                return IntPtr.Zero;

            // En modo claro Windows borra el fondo en blanco opaco y tapa la transparencia;
            // se indica que ya está "borrado" para que solo se vea lo que dibuja XAML.
            case NativeMethods.WM_ERASEBKGND:
                return new IntPtr(1);
        }

        return NativeMethods.DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IslandViewModel.IsOpen))
        {
            return;
        }

        if (ViewModel.IsOpen)
        {
            ShowIsland();
        }
        else
        {
            HideIsland();
        }
    }

    private void ShowIsland()
    {
        PositionAtTopCenter();
        AppWindow.Show(activateWindow: false);
        BringToTop();
        _topmostTimer.Start();
        Animate(toOpacity: 1, toScale: 1, toTranslateY: 0, onCompleted: null);
    }

    private void HideIsland()
    {
        _topmostTimer.Stop();
        Animate(toOpacity: 0, toScale: 0.9, toTranslateY: -8, onCompleted: () =>
        {
            if (!ViewModel.IsOpen)
            {
                AppWindow.Hide();
            }
        });
    }

    private void BringToTop() =>
        NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);

    private void PositionAtTopCenter()
    {
        var scale = NativeMethods.GetDpiForWindow(_hwnd) / 96.0;
        var width = Scale(NotchWidth, scale);
        var height = Scale(NotchHeight, scale);

        // OuterBounds (no WorkArea) para quedar pegado al borde físico superior de la pantalla.
        var bounds = DisplayArea.Primary.OuterBounds;
        var x = bounds.X + (bounds.Width - width) / 2;

        AppWindow.MoveAndResize(new RectInt32(x, bounds.Y, width, height));
    }

    private static int Scale(int value, double scale) => (int)Math.Round(value * scale);

    private void Animate(double toOpacity, double toScale, double toTranslateY, Action? onCompleted)
    {
        _runningStoryboard?.Stop();

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();
        AddAnimation(storyboard, IslandContent, "Opacity", toOpacity, easing);
        AddAnimation(storyboard, ContentTransform, "ScaleX", toScale, easing);
        AddAnimation(storyboard, ContentTransform, "ScaleY", toScale, easing);
        AddAnimation(storyboard, ContentTransform, "TranslateY", toTranslateY, easing);

        if (onCompleted is not null)
        {
            storyboard.Completed += (_, _) => onCompleted();
        }

        _runningStoryboard = storyboard;
        storyboard.Begin();
    }

    private static void AddAnimation(Storyboard storyboard, DependencyObject target, string property, double to, EasingFunctionBase easing)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = AnimationDuration,
            EasingFunction = easing,
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        storyboard.Children.Add(animation);
    }
}
