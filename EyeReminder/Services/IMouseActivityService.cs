namespace EyeReminder.Services;

/// <summary>Detecta movimiento del mouse en todo el sistema, no solo sobre la app.</summary>
public interface IMouseActivityService
{
    event EventHandler? MouseMoved;

    void Start();

    void Stop();
}
