namespace Dsw2026Tpi.Domain.Rules;

/// <summary>
/// Centraliza las reglas generales aplicadas
/// a las especialidades médicas.
/// </summary>
public static class SpecialtyRules
{
    /// <summary>
    /// Longitud mínima admitida para el nombre.
    /// </summary>
    public const int MinimumNameLength = 3;

    /// <summary>
    /// Longitud máxima admitida para el nombre.
    /// </summary>
    public const int MaximumNameLength = 100;

    /// <summary>
    /// Longitud mínima admitida para la descripción.
    /// </summary>
    public const int MinimumDescriptionLength = 10;

    /// <summary>
    /// Longitud máxima admitida para la descripción.
    /// </summary>
    public const int MaximumDescriptionLength = 100;
}