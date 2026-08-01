using Dsw2026Tpi.Api.Responses;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Api.Configurations
{
    public static class RateLimitingConfigurationExtensions
    {
        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // Detalle 1: 429, no el 503 por defecto
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Ventana fija: 100 requests por minuto
                options.AddFixedWindowLimiter("fixed", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Detalle 2: el cuerpo del 429 en el formato de error de la API
                options.OnRejected = async (context, token) =>
                {
                    var error = new ErrorResponse(
                        nameof(ErrorCodes.RATE_LIMIT_EXCEEDED),
                        ErrorCodes.RATE_LIMIT_EXCEEDED);

                    await ErrorResponseWriter.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status429TooManyRequests,
                        error);
                };
            });

            return services;
        }
    }

}
