using Microsoft.AspNetCore.HttpOverrides;

namespace Dsw2026Tpi.Api.Configurations;

public static class ForwardedHeadersConfigurationExtensions
{
    public static IServiceCollection AddAppForwardedHeaders(
        this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(
            options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                options.ForwardLimit = 1;
            });

        return services;
    }
}