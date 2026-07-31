namespace Dsw2026Tpi.Domain.Rules;

/// <summary>
/// Contiene las reglas generales de las disponibilidades médicas.
/// </summary>
public static class AvailabilityRules
{
    public static readonly TimeSpan SlotDuration =
        TimeSpan.FromMinutes(30);
}