using Dsw2026Tpi.Api.Responses;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Api.Middlewares;

/// <summary>
/// Intercepta las excepciones producidas durante una solicitud
/// y las transforma en respuestas HTTP uniformes.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Inicializa el middleware con el siguiente componente
    /// del pipeline y el servicio de logging.
    /// </summary>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el siguiente componente del pipeline
    /// y controla cualquier excepción producida.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException exception)
        {
            /*
             * Las AppException representan errores controlados
             * por la aplicación.
             */
            _logger.LogWarning(
                exception,
                "La solicitud fue rechazada por una condición controlada. " +
                "Path: {Path}",
                context.Request.Path);

            await HandleExceptionAsync(
                context,
                exception);
        }
        catch (Exception exception)
        {
            /*
             * Toda excepción que no sea AppException representa
             * un error inesperado del servidor.
             */
            _logger.LogError(
                exception,
                "Se produjo un error no controlado. Path: {Path}",
                context.Request.Path);

            await HandleExceptionAsync(
                context,
                exception);
        }
    }

    /// <summary>
    /// Selecciona el status y el error correspondientes
    /// y delega la escritura al componente común.
    /// </summary>
    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var errorResult = MapException(
            exception);

        await ErrorResponseWriter.WriteAsync(
            context,
            errorResult.StatusCode,
            errorResult.Error);
    }

    /// <summary>
    /// Traduce las excepciones conocidas al status HTTP
    /// correspondiente.
    ///
    /// Las excepciones desconocidas siempre se convierten
    /// en un error interno genérico.
    /// </summary>
    private static ErrorResult MapException(
        Exception exception)
    {
        return exception switch
        {
            ValidationException validationException =>
                new ErrorResult(
                    StatusCodes.Status400BadRequest,
                    validationException.Error),


            AuthenticationException authenticationException =>
                new ErrorResult(
                    StatusCodes.Status401Unauthorized,
                    authenticationException.Error),

            AuthorizationException authorizationException =>
                new ErrorResult(
                    StatusCodes.Status403Forbidden,
                    authorizationException.Error),

            EntityNotFoundException notFoundException =>
                new ErrorResult(
                    StatusCodes.Status404NotFound,
                    notFoundException.Error),

            ConflictException conflictException =>
                new ErrorResult(
                    StatusCodes.Status409Conflict,
                    conflictException.Error),

            _ =>
                new ErrorResult(
                    StatusCodes.Status500InternalServerError,
                    new ErrorResponse(
                        nameof(ErrorCodes.UNHANDLED_ERROR),
                        ErrorCodes.UNHANDLED_ERROR))
        };
    }

    /// <summary>
    /// Agrupa el status HTTP y el body que deben enviarse.
    /// Se utiliza únicamente dentro del middleware.
    /// </summary>
    private sealed record ErrorResult(
        int StatusCode,
        ErrorResponse Error);
}