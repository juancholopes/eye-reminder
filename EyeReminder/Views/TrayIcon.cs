using System.ComponentModel;
using EyeReminder.Interop;
using EyeReminder.ViewModels;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace EyeReminder.Views;

/// <summary>
/// Icono en la bandeja del sistema. WinUI 3 no trae uno, así que se usa Shell_NotifyIcon y se
/// reciben sus mensajes subclasificando la ventana principal (que sigue existiendo aunque esté oculta).
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private const int IconId = 1;
    private const uint CallbackMessage = NativeMethods.WM_APP + 1;

    private const uint OpenCommandId = 1;
    private const uint ToggleRunCommandId = 2;
    private const uint BreakNowCommandId = 3;
    private const uint SkipBreakCommandId = 4;
    private const uint ExitCommandId = 5;

    private readonly Window _window;
    private readonly MainViewModel _viewModel;
    private readonly Action _exit;
    private readonly IntPtr _hwnd;
    private readonly IntPtr _icon;
    private readonly uint _taskbarCreatedMessage;

    // Se guarda en un campo para que el GC no libere el delegado mientras Win32 lo usa.
    private readonly NativeMethods.SubclassProc _subclassProc;

    private bool _hasShownStillRunningTip;
    private bool _disposed;

    public TrayIcon(Window window, MainViewModel viewModel, string iconPath, Action exit)
    {
        _window = window;
        _viewModel = viewModel;
        _exit = exit;
        _hwnd = WindowNative.GetWindowHandle(window);

        var size = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSMICON);
        _icon = NativeMethods.LoadImage(IntPtr.Zero, iconPath, NativeMethods.IMAGE_ICON, size, size, NativeMethods.LR_LOADFROMFILE);

        // Explorer avisa con este mensaje cuando se reinicia; hay que volver a agregar el icono.
        _taskbarCreatedMessage = NativeMethods.RegisterWindowMessage("TaskbarCreated");

        _subclassProc = WindowProc;
        NativeMethods.SetWindowSubclass(_hwnd, _subclassProc, UIntPtr.Zero, UIntPtr.Zero);

        AddIcon();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>Avisa una sola vez que la app sigue corriendo al cerrar la ventana.</summary>
    public void ShowStillRunningTip()
    {
        if (_hasShownStillRunningTip)
        {
            return;
        }

        _hasShownStillRunningTip = true;
        var data = CreateData(NativeMethods.NIF_INFO);
        data.szInfoTitle = "Eye Reminder sigue activo";
        data.szInfo = "Te seguiré recordando descansar la vista. Abre o cierra la app desde este icono.";
        data.dwInfoFlags = NativeMethods.NIIF_USER | NativeMethods.NIIF_RESPECT_QUIET_TIME;
        data.hBalloonIcon = _icon;
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref data);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        var data = CreateData(0);
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref data);
        NativeMethods.RemoveWindowSubclass(_hwnd, _subclassProc, UIntPtr.Zero);
        NativeMethods.DestroyIcon(_icon);
    }

    private void AddIcon()
    {
        var data = CreateData(NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_SHOWTIP);
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref data);

        data.uVersion = NativeMethods.NOTIFYICON_VERSION_4;
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_SETVERSION, ref data);
    }

    private NativeMethods.NOTIFYICONDATAW CreateData(int flags) => new()
    {
        cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.NOTIFYICONDATAW>(),
        hWnd = _hwnd,
        uID = IconId,
        uFlags = flags,
        uCallbackMessage = CallbackMessage,
        hIcon = _icon,
        szTip = _viewModel.TrayToolTip,
        szInfo = string.Empty,
        szInfoTitle = string.Empty,
    };

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.TrayToolTip))
        {
            return;
        }

        var data = CreateData(NativeMethods.NIF_TIP | NativeMethods.NIF_SHOWTIP);
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref data);
    }

    private IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr refData)
    {
        if (message == CallbackMessage)
        {
            // Con NOTIFYICON_VERSION_4 el evento va en la palabra baja de lParam
            // y las coordenadas del clic en wParam.
            var trayEvent = (uint)(lParam.ToInt64() & 0xFFFF);
            switch (trayEvent)
            {
                case NativeMethods.NIN_SELECT:
                case NativeMethods.NIN_KEYSELECT:
                case NativeMethods.WM_LBUTTONDBLCLK:
                    OpenWindow();
                    break;
                case NativeMethods.WM_CONTEXTMENU:
                    var x = (short)(wParam.ToInt64() & 0xFFFF);
                    var y = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                    ShowContextMenu(x, y);
                    break;
            }

            return IntPtr.Zero;
        }

        if (message == _taskbarCreatedMessage)
        {
            AddIcon();
        }

        return NativeMethods.DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private void ShowContextMenu(int x, int y)
    {
        var menu = NativeMethods.CreatePopupMenu();
        AppendItem(menu, OpenCommandId, "Abrir Eye Reminder", enabled: true);
        AppendSeparator(menu);
        AppendItem(menu, ToggleRunCommandId, _viewModel.ToggleRunLabel, _viewModel.ToggleRunCommand.CanExecute(null));
        AppendItem(menu, BreakNowCommandId, "Descansar ahora", _viewModel.BreakNowCommand.CanExecute(null));
        if (_viewModel.IsOnBreak)
        {
            AppendItem(menu, SkipBreakCommandId, "Omitir este descanso", enabled: true);
        }

        AppendSeparator(menu);
        AppendItem(menu, ExitCommandId, "Salir", enabled: true);
        NativeMethods.SetMenuDefaultItem(menu, OpenCommandId, byPosition: false);

        // Sin esto el menú no se cierra al hacer clic fuera de él.
        NativeMethods.SetForegroundWindow(_hwnd);
        var command = NativeMethods.TrackPopupMenuEx(menu,
            NativeMethods.TPM_RETURNCMD | NativeMethods.TPM_RIGHTBUTTON | NativeMethods.TPM_BOTTOMALIGN,
            x, y, _hwnd, IntPtr.Zero);
        NativeMethods.PostMessage(_hwnd, NativeMethods.WM_NULL, IntPtr.Zero, IntPtr.Zero);
        NativeMethods.DestroyMenu(menu);

        switch (command)
        {
            case OpenCommandId:
                OpenWindow();
                break;
            case ToggleRunCommandId:
                _viewModel.ToggleRunCommand.Execute(null);
                break;
            case BreakNowCommandId:
                _viewModel.BreakNowCommand.Execute(null);
                break;
            case SkipBreakCommandId:
                _viewModel.SkipBreakCommand.Execute(null);
                break;
            case ExitCommandId:
                _exit();
                break;
        }
    }

    private static void AppendItem(IntPtr menu, uint id, string text, bool enabled) =>
        NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING | (enabled ? 0 : NativeMethods.MF_GRAYED), id, text);

    private static void AppendSeparator(IntPtr menu) =>
        NativeMethods.AppendMenu(menu, NativeMethods.MF_SEPARATOR, UIntPtr.Zero, null);

    private void OpenWindow()
    {
        _window.AppWindow.Show();
        _window.Activate();
        NativeMethods.SetForegroundWindow(_hwnd);
    }
}
