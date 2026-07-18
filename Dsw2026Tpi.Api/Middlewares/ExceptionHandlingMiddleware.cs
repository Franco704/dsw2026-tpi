using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;
using System.Text.Json;

namespace Dsw2026Tpi.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Se produjo un error durante el procesamiento de la solicitud");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        ErrorResponse error = ex is AppException exApp ? 
            exApp.Error : 
            new ErrorResponse(nameof(ErrorCodes.UNHANDLED_ERROR), ErrorCodes.UNHANDLED_ERROR);
        // Determina el código HTTP según el tipo de excepción.
        var status = ex switch
        {
            // Los datos recibidos no cumplen las validaciones.
            ValidationException
                => HttpStatusCode.BadRequest,

            // La entidad solicitada no existe.
            EntityNotFoundException
                => HttpStatusCode.NotFound,

            // Existe un conflicto con el estado actual del sistema.
            ConflictException
                => HttpStatusCode.Conflict,

            // No se pudieron validar las credenciales.
            AuthenticationException
                => HttpStatusCode.Unauthorized,

            // El usuario está autenticado, pero no tiene permisos.
            AuthorizationException
                => HttpStatusCode.Forbidden,

            // Todo error no controlado devuelve 500.
            _ => HttpStatusCode.InternalServerError,
        };
        var result = JsonSerializer.Serialize(error);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(result);
    }
}
