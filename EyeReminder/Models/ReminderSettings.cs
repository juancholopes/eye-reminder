namespace EyeReminder.Models;

/// <summary>
/// Parámetros de la regla 20-20-20: cada <see cref="WorkInterval"/> se descansa
/// <see cref="BreakDuration"/> mirando algo a <see cref="FocusDistanceFeet"/> pies.
/// </summary>
public sealed record ReminderSettings(TimeSpan WorkInterval, TimeSpan BreakDuration, int FocusDistanceFeet)
{
    public static ReminderSettings Default { get; } =
        new(TimeSpan.FromMinutes(20), TimeSpan.FromSeconds(20), 20);

    /// <summary>Intervalo corto para probar la app sin esperar 20 minutos (argumento --demo).</summary>
    public static ReminderSettings Demo { get; } =
        new(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20), 20);

    public double FocusDistanceMeters => Math.Round(FocusDistanceFeet * 0.3048, 1);
}
