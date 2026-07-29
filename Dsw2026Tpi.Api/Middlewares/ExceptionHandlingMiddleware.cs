using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;
using System.Text.Json;

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
    /// Inicializa la middleware con el siguiente componente
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
             * Las AppException representan situaciones controladas:
             * validaciones, conflictos, autenticación, autorización
             * o entidades inexistentes.
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
    /// Determina el estado HTTP correspondiente y escribe
    /// la respuesta utilizando el formato común de errores.
    /// </summary>
    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        /*
         * Las excepciones de aplicación ya contienen su
         * ErrorResponse específico.
         *
         * Las excepciones inesperadas se reemplazan por un
         * mensaje genérico para no revelar información interna.
         */
        var error = exception is AppException appException
            ? appException.Error
            : new ErrorResponse(
                nameof(ErrorCodes.UNHANDLED_ERROR),
                ErrorCodes.UNHANDLED_ERROR);

        var status = exception switch
        {
            ValidationException
                => HttpStatusCode.BadRequest,

            EntityNotFoundException
                => HttpStatusCode.NotFound,

            ConflictException
                => HttpStatusCode.Conflict,

            AuthenticationException
                => HttpStatusCode.Unauthorized,

            AuthorizationException
                => HttpStatusCode.Forbidden,

            _
                => HttpStatusCode.InternalServerError
        };

        var result = JsonSerializer.Serialize(
            error);

        context.Response.ContentType =
            "application/json";

        context.Response.StatusCode =
            (int)status;

        await context.Response.WriteAsync(
            result);
    }
}