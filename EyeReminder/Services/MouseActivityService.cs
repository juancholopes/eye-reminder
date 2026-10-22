using EyeReminder.Interop;
using Microsoft.UI.Dispatching;

namespace EyeReminder.Services;

/// <summary>
/// Consulta la posición global del cursor cada 100 ms. Es más simple y seguro que un
/// hook de bajo nivel y basta para saber si el usuario sigue usando el mouse.
/// </summary>
public sealed class MouseActivityService : IMouseActivityService
{
    // Tolerancia en píxeles para ignorar vibraciones mínimas del sensor.
    private const int MovementThreshold = 2;

    private readonly DispatcherQueueTimer _timer;
    private NativeMethods.POINT _lastPosition;

    public MouseActivityService(DispatcherQueue dispatcher)
    {
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += OnTimerTick;
    }

    public event EventHandler? MouseMoved;

    public void Start()
    {
        NativeMethods.GetCursorPos(out _lastPosition);
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (!NativeMethods.GetCursorPos(out var position))
        {
            return;
        }

        var moved = Math.Abs(position.X - _lastPosition.X) > MovementThreshold
                    || Math.Abs(position.Y - _lastPosition.Y) > MovementThreshold;
        if (!moved)
        {
            return;
        }

        _lastPosition = position;
        MouseMoved?.Invoke(this, EventArgs.Empty);
    }
}
