using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyeReminder.Models;
using EyeReminder.Services;

namespace EyeReminder.ViewModels;

/// <summary>Coordina el ciclo 20-20-20: trabajo → isla de descanso → trabajo.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IReminderScheduler _scheduler;
    private readonly IslandViewModel _island;
    private readonly ReminderSettings _settings;

    // Indica si el usuario quiere que el ciclo siga después de cada descanso.
    private bool _wantsRunning;

    // Indica si la sesión de Windows está bloqueada.
    private bool _isLocked;

    public MainViewModel(IReminderScheduler scheduler, IslandViewModel island, ReminderSettings settings)
    {
        _scheduler = scheduler;
        _island = island;
        _settings = settings;

        _scheduler.Tick += (_, _) => RefreshCountdown();
        _scheduler.ReminderDue += (_, _) => BeginBreak();
        _island.Completed += (_, _) => OnBreakCompleted();

        RefreshCountdown();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrayToolTip))]
    public partial string CountdownText { get; private set; } = "20:00";

    /// <summary>Porcentaje del intervalo de trabajo ya transcurrido.</summary>
    [ObservableProperty]
    public partial double ElapsedProgress { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(ToggleRunLabel), nameof(ToggleRunGlyph), nameof(TrayToolTip))]
    [NotifyCanExecuteChangedFor(nameof(BreakNowCommand), nameof(SkipBreakCommand), nameof(ToggleRunCommand))]
    public partial bool IsOnBreak { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(ToggleRunLabel), nameof(ToggleRunGlyph), nameof(TrayToolTip))]
    public partial bool IsRunning { get; private set; }

    [ObservableProperty]
    public partial int BreaksCompleted { get; private set; }

    public string StatusText => IsOnBreak
        ? "En descanso: mira a lo lejos y no muevas el mouse"
        : IsRunning ? "Próximo descanso en" : "Recordatorio en pausa";

    public string ToggleRunLabel => IsRunning ? "Pausar" : "Reanudar";

    public string TrayToolTip => IsOnBreak
        ? "Eye Reminder · En descanso"
        : IsRunning ? $"Eye Reminder · Próximo descanso en {CountdownText}" : "Eye Reminder · En pausa";

    // Segoe Fluent Icons: Pause / Play
    public string ToggleRunGlyph => IsRunning ? "" : "";

    public void Start()
    {
        _wantsRunning = true;
        _scheduler.Start();
        IsRunning = true;
    }

    [RelayCommand(CanExecute = nameof(CanToggleRun))]
    private void ToggleRun()
    {
        if (IsRunning)
        {
            _wantsRunning = false;
            _scheduler.Pause();
            IsRunning = false;
        }
        else
        {
            Start();
        }
    }

    private bool CanToggleRun() => !IsOnBreak;

    [RelayCommand(CanExecute = nameof(CanBreakNow))]
    private void BreakNow()
    {
        _scheduler.Pause();
        BeginBreak();
    }

    private bool CanBreakNow() => !IsOnBreak;

    [RelayCommand(CanExecute = nameof(CanSkipBreak))]
    private void SkipBreak()
    {
        _island.Cancel();
        EndBreak();
    }

    private bool CanSkipBreak() => IsOnBreak;

    private void BeginBreak()
    {
        IsOnBreak = true;
        IsRunning = false;
        _island.Start();
    }

    private void OnBreakCompleted()
    {
        BreaksCompleted++;
        EndBreak();
    }

    /// <summary>
    /// Con la sesión bloqueada no hay nadie frente a la pantalla: el tiempo no cuenta.
    /// Si había un descanso en curso se descarta sin contarlo como completado.
    /// </summary>
    public void SuspendForLock()
    {
        _isLocked = true;
        if (IsOnBreak)
        {
            _island.Cancel();
            IsOnBreak = false;
        }

        _scheduler.Pause();
        IsRunning = false;
    }

    /// <summary>Al desbloquear los 20 minutos empiezan desde cero, salvo que el usuario lo haya pausado.</summary>
    public void ResumeAfterUnlock()
    {
        if (!_isLocked)
        {
            return;
        }

        _isLocked = false;
        RestartCycle();
    }

    private void EndBreak()
    {
        IsOnBreak = false;
        RestartCycle();
    }

    private void RestartCycle()
    {
        _scheduler.Reset();
        if (_wantsRunning)
        {
            _scheduler.Start();
            IsRunning = true;
        }
    }

    private void RefreshCountdown()
    {
        var remaining = _scheduler.Remaining;
        CountdownText = remaining.ToString(remaining.TotalHours >= 1 ? @"h\:mm\:ss" : @"mm\:ss");
        ElapsedProgress = 100 - remaining.TotalMilliseconds / _scheduler.Interval.TotalMilliseconds * 100;
    }
}
