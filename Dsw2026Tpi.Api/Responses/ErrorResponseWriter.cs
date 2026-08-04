using Dsw2026Tpi.CrossCutting.Models;
using System.Text.Json;

namespace Dsw2026Tpi.Api.Responses;



public static class ErrorResponseWriter
{

    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase
        };

    
    
    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        ErrorResponse error)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(error);


        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();

        context.Response.StatusCode =
            statusCode;

        context.Response.ContentType =
            "application/json";

        var json = JsonSerializer.Serialize(
            error,
            SerializerOptions);

        await context.Response.WriteAsync(
            json,
            context.RequestAborted);
    }
}