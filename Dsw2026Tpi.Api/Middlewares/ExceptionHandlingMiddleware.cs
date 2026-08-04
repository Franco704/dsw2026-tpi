using Dsw2026Tpi.Api.Responses;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException exception)
        {
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
            _logger.LogError(
                exception,
                "Se produjo un error no controlado. Path: {Path}",
                context.Request.Path);

            await HandleExceptionAsync(
                context,
                exception);
        }
    }

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

    private sealed record ErrorResult(
        int StatusCode,
        ErrorResponse Error);
}