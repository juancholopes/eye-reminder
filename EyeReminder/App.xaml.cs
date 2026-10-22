using EyeReminder.Models;
using EyeReminder.Services;
using EyeReminder.ViewModels;
using EyeReminder.Views;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace EyeReminder;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private IslandWindow? _islandWindow;
    private TrayIcon? _trayIcon;
    private MouseActivityService? _mouseMonitor;
    private bool _isExiting;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Raíz de composición: aquí se crean y conectan servicios, ViewModels y vistas.
        var dispatcher = DispatcherQueue.GetForCurrentThread();
        var settings = Environment.GetCommandLineArgs().Contains("--demo")
            ? ReminderSettings.Demo
            : ReminderSettings.Default;

        var scheduler = new ReminderScheduler(dispatcher, settings.WorkInterval);
        _mouseMonitor = new MouseActivityService(dispatcher);

        var islandViewModel = new IslandViewModel(dispatcher, _mouseMonitor, settings);
        var mainViewModel = new MainViewModel(scheduler, islandViewModel, settings);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "EyeReminder.ico");

        _islandWindow = new IslandWindow(islandViewModel);
        _mainWindow = new MainWindow(mainViewModel);
        _mainWindow.AppWindow.SetIcon(iconPath);
        _trayIcon = new TrayIcon(_mainWindow, mainViewModel, iconPath, ExitApp);

        // Cerrar la ventana solo la oculta: la app sigue en la bandeja del sistema.
        _mainWindow.AppWindow.Closing += (sender, e) =>
        {
            if (_isExiting)
            {
                return;
            }

            e.Cancel = true;
            sender.Hide();
            _trayIcon.ShowStillRunningTip();
        };

        _mainWindow.Activate();
        mainViewModel.Start();
    }

    private void ExitApp()
    {
        _isExiting = true;
        _trayIcon?.Dispose();
        _mouseMonitor?.Stop();
        _islandWindow?.Close();
        _mainWindow?.Close();
        Exit();
    }
}
