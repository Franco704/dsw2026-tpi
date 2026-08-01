namespace Dsw2026Tpi.Api.Configurations;

/// <summary>
/// Contiene los nombres de las políticas de rate limiting
/// utilizadas por los endpoints de la API.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Política aplicada a todos los endpoints que no poseen
    /// una limitación específica.
    /// </summary>
    public const string General = "General";

    /// <summary>
    /// Política aplicada al inicio de sesión de administradores.
    /// </summary>
    public const string AdminLogin = "AdminLogin";

    /// <summary>
    /// Política aplicada al inicio de sesión de pacientes.
    /// </summary>
    public const string PatientLogin = "PatientLogin";

    /// <summary>
    /// Política aplicada a la reserva de turnos.
    /// </summary>
    public const string AppointmentBooking = "AppointmentBooking";
}