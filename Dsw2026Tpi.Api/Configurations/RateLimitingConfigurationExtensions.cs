using Dsw2026Tpi.Api.Responses;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Dsw2026Tpi.Api.Configurations;

public static class RateLimitingConfigurationExtensions
{
    public static IServiceCollection AddAppRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(
            configuration);

        var settings = configuration
            .GetRequiredSection(
                RateLimitingSettings.SectionName)
            .Get<RateLimitingSettings>()
            ?? throw new InvalidOperationException(
                "No se pudo obtener la configuración de rate limiting.");

        settings.Validate();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            AddIpPolicy(
                options,
                RateLimitPolicies.AdminLogin,
                settings.AdminLogin);

            AddIpPolicy(
                options,
                RateLimitPolicies.PatientLogin,
                settings.PatientLogin);

            AddUserOrIpPolicy(
                options,
                RateLimitPolicies.General,
                settings.General);

            AddUserOrIpPolicy(
                options,
                RateLimitPolicies.AppointmentBooking,
                settings.AppointmentBooking);

            options.OnRejected =
                HandleRejectedRequestAsync;
        });

        return services;
    }

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

    private static string GetIpPartitionKey(
        HttpContext context)
    {
        var ipAddress =
            context.Connection.RemoteIpAddress?
                .ToString();

        return $"ip:{ipAddress ?? "unknown"}";
    }

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