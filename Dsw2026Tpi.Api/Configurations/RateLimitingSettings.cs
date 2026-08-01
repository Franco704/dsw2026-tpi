namespace Dsw2026Tpi.Api.Configurations;

/// <summary>
/// Representa la configuración completa de rate limiting
/// obtenida desde los archivos de configuración de la API.
/// </summary>
public sealed class RateLimitingSettings
{
    /// <summary>
    /// Nombre de la sección utilizada en appsettings.
    /// </summary>
    public const string SectionName = "RateLimiting";

    public RateLimitPolicySettings General { get; init; } = new();

    public RateLimitPolicySettings AdminLogin { get; init; } = new();

    public RateLimitPolicySettings PatientLogin { get; init; } = new();

    public RateLimitPolicySettings AppointmentBooking { get; init; } = new();

    /// <summary>
    /// Comprueba que todas las políticas tengan
    /// valores utilizables y no permitan encolar solicitudes.
    /// </summary>
    public void Validate()
    {
        General.Validate(
            nameof(General));

        AdminLogin.Validate(
            nameof(AdminLogin));

        PatientLogin.Validate(
            nameof(PatientLogin));

        AppointmentBooking.Validate(
            nameof(AppointmentBooking));
    }
}

/// <summary>
/// Representa los valores configurables de una política
/// de ventana fija.
/// </summary>
public sealed class RateLimitPolicySettings
{
    /// <summary>
    /// Cantidad máxima de solicitudes permitidas
    /// dentro de la ventana configurada.
    /// </summary>
    public int PermitLimit { get; init; }

    /// <summary>
    /// Duración de la ventana expresada en segundos.
    /// </summary>
    public int WindowInSeconds { get; init; }

    /// <summary>
    /// Cantidad de solicitudes que pueden permanecer en espera.
    /// Para este proyecto debe ser siempre cero.
    /// </summary>
    public int QueueLimit { get; init; }

    /// <summary>
    /// Valida los valores obtenidos desde appsettings.
    /// La aplicación no debe iniciar con una configuración inválida.
    /// </summary>
    public void Validate(
        string policyName)
    {
        if (PermitLimit <= 0)
        {
            throw new InvalidOperationException(
                $"RateLimiting:{policyName}:PermitLimit debe ser mayor que cero.");
        }

        if (WindowInSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"RateLimiting:{policyName}:WindowInSeconds debe ser mayor que cero.");
        }

        if (QueueLimit != 0)
        {
            throw new InvalidOperationException(
                $"RateLimiting:{policyName}:QueueLimit debe ser cero.");
        }
    }
}