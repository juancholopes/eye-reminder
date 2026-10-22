namespace EyeReminder.Services;

/// <summary>Avisa cuando el usuario bloquea o desbloquea la sesión de Windows.</summary>
public interface ISessionLockService
{
    event EventHandler? Locked;

    event EventHandler? Unlocked;
}
