using Dsw2026Tpi.Api.Responses;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Dsw2026Tpi.Api.Configurations;

/// <summary>
/// Configura las políticas de rate limiting utilizadas por la API.
/// Los límites se obtienen desde appsettings.
/// </summary>
public static class RateLimitingConfigurationExtensions
{
    /// <summary>
    /// Registra las políticas generales y específicas
    /// requeridas por el contrato de la API.
    /// </summary>
    public static IServiceCollection AddAppRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(
            configuration);

        /*
         * Obtiene la configuración completa y detiene
         * el inicio si la sección no existe.
         */
        var settings = configuration
            .GetRequiredSection(
                RateLimitingSettings.SectionName)
            .Get<RateLimitingSettings>()
            ?? throw new InvalidOperationException(
                "No se pudo obtener la configuración de rate limiting.");

        /*
         * Comprueba los límites antes de registrar
         * las políticas en el contenedor.
         */
        settings.Validate();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            /*
             * Los logins se limitan por dirección IP
             * porque todavía no existe un usuario autenticado.
             */
            AddIpPolicy(
                options,
                RateLimitPolicies.AdminLogin,
                settings.AdminLogin);

            AddIpPolicy(
                options,
                RateLimitPolicies.PatientLogin,
                settings.PatientLogin);

            /*
             * La política general utiliza el usuario autenticado.
             * Para solicitudes anónimas utiliza la dirección IP.
             */
            AddUserOrIpPolicy(
                options,
                RateLimitPolicies.General,
                settings.General);

            /*
             * La reserva utiliza el usuario autenticado
             * para mantener un contador independiente por paciente.
             */
            AddUserOrIpPolicy(
                options,
                RateLimitPolicies.AppointmentBooking,
                settings.AppointmentBooking);

            options.OnRejected =
                HandleRejectedRequestAsync;
        });

        return services;
    }

    /// <summary>
    /// Registra una política de ventana fija
    /// particionada por dirección IP.
    /// </summary>
    private static void AddIpPolicy(
        RateLimiterOptions options,
        string policyName,
        RateLimitPolicySettings settings)
    {
        options.AddPolicy(
            policyName,
            httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetIpPartitionKey(
                        httpContext),
                    _ => CreateFixedWindowOptions(
                        settings)));
    }

    /// <summary>
    /// Registra una política particionada por usuario autenticado.
    /// Si la solicitud es anónima, utiliza su dirección IP.
    /// </summary>
    private static void AddUserOrIpPolicy(
        RateLimiterOptions options,
        string policyName,
        RateLimitPolicySettings settings)
    {
        options.AddPolicy(
            policyName,
            httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetUserOrIpPartitionKey(
                        httpContext),
                    _ => CreateFixedWindowOptions(
                        settings)));
    }

    /// <summary>
    /// Construye las opciones de una ventana fija
    /// a partir de los valores obtenidos desde appsettings.
    /// </summary>
    private static FixedWindowRateLimiterOptions
        CreateFixedWindowOptions(
            RateLimitPolicySettings settings)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit =
                settings.PermitLimit,

            Window =
                TimeSpan.FromSeconds(
                    settings.WindowInSeconds),

            QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst,

            QueueLimit =
                settings.QueueLimit,

            AutoReplenishment =
                true
        };
    }

    /// <summary>
    /// Obtiene una clave estable a partir
    /// de la dirección IP de la solicitud.
    /// </summary>
    private static string GetIpPartitionKey(
        HttpContext context)
    {
        var ipAddress =
            context.Connection.RemoteIpAddress?
                .ToString();

        return $"ip:{ipAddress ?? "unknown"}";
    }

    /// <summary>
    /// Obtiene una clave por usuario autenticado
    /// o utiliza la IP cuando no existe identidad.
    /// </summary>
    private static string GetUserOrIpPartitionKey(
        HttpContext context)
    {
        var identity =
            context.User.Identity;

        if (identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(
                identity.Name))
        {
            var normalizedUser =
                identity.Name
                    .Trim()
                    .ToLowerInvariant();

            return $"user:{normalizedUser}";
        }

        return GetIpPartitionKey(
            context);
    }

    /// <summary>
    /// Registra el rechazo y devuelve el body
    /// contractual con status HTTP 429.
    /// </summary>
    private static async ValueTask
        HandleRejectedRequestAsync(
            OnRejectedContext context,
            CancellationToken _)
    {
        var httpContext =
            context.HttpContext;

        var loggerFactory =
            httpContext.RequestServices
                .GetRequiredService<ILoggerFactory>();

        var logger =
            loggerFactory.CreateLogger(
                "RateLimiting");

        logger.LogWarning(
            "Solicitud rechazada por rate limiting. " +
            "Method: {Method}, Path: {Path}, IP: {IpAddress}, User: {User}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Connection.RemoteIpAddress?
                .ToString() ?? "unknown",
            httpContext.User.Identity?.Name ??
                "anonymous");

        var error = new ErrorResponse(
            nameof(
                ErrorCodes.RATE_LIMIT_EXCEEDED),
            ErrorCodes.RATE_LIMIT_EXCEEDED);

        await ErrorResponseWriter.WriteAsync(
            httpContext,
            StatusCodes.Status429TooManyRequests,
            error);
    }
}