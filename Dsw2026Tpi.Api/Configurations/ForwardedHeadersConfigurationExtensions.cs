using Microsoft.AspNetCore.HttpOverrides;

namespace Dsw2026Tpi.Api.Configurations;

/// <summary>
/// Configura el procesamiento de los headers enviados
/// por proxies como ngrok.
/// </summary>
public static class ForwardedHeadersConfigurationExtensions
{
    /// <summary>
    /// Permite obtener la IP y el esquema originales
    /// de una solicitud reenviada por un proxy confiable.
    /// </summary>
    public static IServiceCollection AddAppForwardedHeaders(
        this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(
            options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                /*
                 * Ngrok representa un único proxy entre
                 * el cliente y la API local.
                 */
                options.ForwardLimit = 1;
            });

        return services;
    }
}