using CommunityToolkit.Mvvm.ComponentModel;
using EyeReminder.Models;
using EyeReminder.Services;
using Microsoft.UI.Dispatching;

namespace EyeReminder.ViewModels;

/// <summary>
/// Estado de la isla dinámica: cuenta 20 segundos y vuelve a empezar cada vez que el mouse se mueve.
/// </summary>
public sealed partial class IslandViewModel : ObservableObject
{
    private static readonly TimeSpan MovementHintDuration = TimeSpan.FromSeconds(1.5);

    private readonly IMouseActivityService _mouseActivity;
    private readonly ReminderSettings _settings;
    private readonly DispatcherQueueTimer _countdownTimer;
    private long _breakEndTicks;
    private long _movementHintEndTicks;

    public IslandViewModel(DispatcherQueue dispatcher, IMouseActivityService mouseActivity, ReminderSettings settings)
    {
        _mouseActivity = mouseActivity;
        _settings = settings;
        _mouseActivity.MouseMoved += OnMouseMoved;

        _countdownTimer = dispatcher.CreateTimer();
        _countdownTimer.Interval = TimeSpan.FromMilliseconds(50);
        _countdownTimer.Tick += (_, _) => UpdateCountdown();

        SecondsRemaining = (int)settings.BreakDuration.TotalSeconds;
        Progress = 100;
    }

    /// <summary>El descanso terminó porque se cumplieron los segundos sin mover el mouse.</summary>
    public event EventHandler? Completed;

    [ObservableProperty]
    public partial bool IsOpen { get; private set; }

    [ObservableProperty]
    public partial int SecondsRemaining { get; private set; }

    /// <summary>Porcentaje restante (100 → 0) para el anillo de progreso.</summary>
    [ObservableProperty]
    public partial double Progress { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle))]
    public partial bool MovementDetected { get; private set; }

    public string Title => "Descansa la vista";

    public string Subtitle => MovementDetected
        ? "Movimiento detectado · reiniciando"
        : $"Mira algo a {_settings.FocusDistanceFeet} pies ({_settings.FocusDistanceMeters:0.#} m)";

    public void Start()
    {
        if (IsOpen)
        {
            return;
        }

        RestartCountdown();
        MovementDetected = false;
        IsOpen = true;
        _mouseActivity.Start();
        _countdownTimer.Start();
    }

    /// <summary>Cierra la isla sin contar el descanso como completado.</summary>
    public void Cancel() => Close(completed: false);

    private void OnMouseMoved(object? sender, EventArgs e)
    {
        if (!IsOpen)
        {
            return;
        }

        RestartCountdown();
        MovementDetected = true;
        _movementHintEndTicks = Environment.TickCount64 + (long)MovementHintDuration.TotalMilliseconds;
    }

    private void RestartCountdown()
    {
        _breakEndTicks = Environment.TickCount64 + (long)_settings.BreakDuration.TotalMilliseconds;
        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        var now = Environment.TickCount64;
        var remainingMs = Math.Max(0, _breakEndTicks - now);
        var totalMs = _settings.BreakDuration.TotalMilliseconds;

        SecondsRemaining = (int)Math.Ceiling(remainingMs / 1000.0);
        Progress = remainingMs / totalMs * 100;

        if (MovementDetected && now >= _movementHintEndTicks)
        {
            MovementDetected = false;
        }

        if (IsOpen && remainingMs == 0)
        {
            Close(completed: true);
        }
    }

    private void Close(bool completed)
    {
        if (!IsOpen)
        {
            return;
        }

        _countdownTimer.Stop();
        _mouseActivity.Stop();
        IsOpen = false;

        if (completed)
        {
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
}
