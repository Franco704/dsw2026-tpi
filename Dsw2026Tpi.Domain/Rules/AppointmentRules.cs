namespace Dsw2026Tpi.Domain.Rules;

/// <summary>
/// Centraliza las reglas generales aplicadas
/// a los turnos médicos.
/// </summary>
public static class AppointmentRules
{
    /// <summary>
    /// Longitud mínima contractual del motivo de consulta.
    /// </summary>
    public const int MinimumReasonLength = 5;

    /// <summary>
    /// Longitud máxima admitida por el modelo persistido.
    /// </summary>
    public const int MaximumReasonLength = 300;
}