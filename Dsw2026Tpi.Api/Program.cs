using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Api.Middlewares;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information(
                "Iniciando aplicación Dsw2026Tpi.Api");

            var builder =
                WebApplication.CreateBuilder(args);

            builder.AddSerilogConfiguration();

            builder.Services.AddAppIdentity();
            builder.Services.AddAppAuthentication(
                builder.Configuration);

            builder.Services.AddSwaggerConfiguration();

            builder.Services.AddApplicationPersistence(
                builder.Configuration);

            builder.Services.AddAppCors(
                builder.Configuration);

            builder.Services.AddAppForwardedHeaders();

            builder.Services.AddAppDependencies();
            builder.Services.AddControllers();


            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var error = new ErrorResponse(
                        nameof(ErrorCodes.VALIDATION_ERROR),
                        ErrorCodes.VALIDATION_ERROR);

                    foreach (var entry in context.ModelState)
                    {
                        if (entry.Value.Errors.Count == 0)
                        {
                            continue;
                        }
                        var field = entry.Key.StartsWith("$.")
                            ? entry.Key[2..]
                            : entry.Key;
                        if (string.IsNullOrWhiteSpace(field))
                        {
                            field = "request";
                        }
                        error.AddDetail(field, "invalid_format");
                    }
                    return new BadRequestObjectResult(error);
                };
            });


            builder.Services.AddHealthChecks();
            builder.Services.AddAppRateLimiting(
                builder.Configuration);
            var app = builder.Build();

            await app.SeedInitialAdminAsync();

            app.UseForwardedHeaders();

            app.UseSerilogRequestLogging();

            if (app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();

            app.UseRateLimiter();

            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks(
                    "/health-check")
                .RequireRateLimiting(
                    RateLimitPolicies.General);

            Log.Information(
                "Aplicación iniciada correctamente");

            await app.RunAsync();
        }
        catch (HostAbortedException)
        {
            Log.Information(
                "El host fue abortado " +
                "(normal durante migraciones de EF Core)");
        }
        catch (Exception exception)
        {
            Log.Fatal(
                exception,
                "La aplicación falló al iniciar");

            throw;
        }
        finally
        {
            Log.Information(
                "Cerrando aplicación");

            await Log.CloseAndFlushAsync();
        }
    }
}