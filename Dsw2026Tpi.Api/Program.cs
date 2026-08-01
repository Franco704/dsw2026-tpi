using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Api.Middlewares;
using Serilog;

namespace Dsw2026Tpi.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Inicializa un logger básico antes de construir la aplicación.
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information(
                "Iniciando aplicación Dsw2026Tpi.Api");

            var builder =
                WebApplication.CreateBuilder(args);

            // Configuraciones personalizadas de la aplicación.
            builder.AddSerilogConfiguration();

            builder.Services.AddAppIdentity();
            builder.Services.AddAppAuthentication(
                builder.Configuration);

            builder.Services.AddSwaggerConfiguration();

            builder.Services.AddApplicationPersistence(
                builder.Configuration);

            builder.Services.AddAppCors(
                builder.Configuration);

            builder.Services.AddAppDependencies();
            builder.Services.AddControllers();
            builder.Services.AddHealthChecks();
            builder.Services.AddAppRateLimiting();

            var app = builder.Build();

            await app.SeedInitialAdminAsync();

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

            /*
             * El middleware de excepciones se registra antes
             * de los componentes que ejecutan los endpoints.
             */
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseCors();

            /*
             * La autenticación debe ejecutarse antes del rate limiter
             * para que posteriormente puedan aplicarse límites
             * utilizando la identidad del usuario.
             */
            app.UseAuthentication();

            app.UseRateLimiter();

            app.UseAuthorization();

            /*
             * Aplica temporalmente la política general de
             * cien solicitudes por minuto a los controladores.
             */
            app.MapControllers()
                .RequireRateLimiting("fixed");

            app.MapHealthChecks(
                "/health-check");

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