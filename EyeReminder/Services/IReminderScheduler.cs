namespace EyeReminder.Services;

/// <summary>Cuenta regresiva del intervalo de trabajo hasta el siguiente descanso.</summary>
public interface IReminderScheduler
{
    TimeSpan Interval { get; }

    TimeSpan Remaining { get; }

    bool IsRunning { get; }

    /// <summary>Se dispara periódicamente mientras el temporizador corre.</summary>
    event EventHandler? Tick;

    /// <summary>Se dispara cuando el intervalo termina. El temporizador queda detenido.</summary>
    event EventHandler? ReminderDue;

    void Start();

    void Pause();

    /// <summary>Vuelve a dejar el intervalo completo sin cambiar si corre o no.</summary>
    void Reset();
}
