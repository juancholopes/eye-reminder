using Microsoft.UI.Dispatching;

namespace EyeReminder.Services;

public sealed class ReminderScheduler : IReminderScheduler
{
    private readonly DispatcherQueueTimer _timer;
    private TimeSpan _remaining;
    private long _deadlineTicks;

    public ReminderScheduler(DispatcherQueue dispatcher, TimeSpan interval)
    {
        Interval = interval;
        _remaining = interval;

        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(250);
        _timer.Tick += OnTimerTick;
    }

    public TimeSpan Interval { get; }

    public TimeSpan Remaining => IsRunning ? ComputeRemaining() : _remaining;

    public bool IsRunning => _timer.IsRunning;

    public event EventHandler? Tick;

    public event EventHandler? ReminderDue;

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        // Se usa el reloj monotónico para no depender de cambios en la hora del sistema.
        _deadlineTicks = Environment.TickCount64 + (long)_remaining.TotalMilliseconds;
        _timer.Start();
        Tick?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        if (!IsRunning)
        {
            return;
        }

        _remaining = ComputeRemaining();
        _timer.Stop();
        Tick?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        _remaining = Interval;
        _deadlineTicks = Environment.TickCount64 + (long)Interval.TotalMilliseconds;
        Tick?.Invoke(this, EventArgs.Empty);
    }

    private TimeSpan ComputeRemaining()
    {
        var milliseconds = Math.Max(0, _deadlineTicks - Environment.TickCount64);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        var remaining = ComputeRemaining();
        if (remaining > TimeSpan.Zero)
        {
            Tick?.Invoke(this, EventArgs.Empty);
            return;
        }

        _timer.Stop();
        _remaining = TimeSpan.Zero;
        Tick?.Invoke(this, EventArgs.Empty);
        ReminderDue?.Invoke(this, EventArgs.Empty);
    }
}
