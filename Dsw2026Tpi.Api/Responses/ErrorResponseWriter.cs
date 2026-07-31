using Dsw2026Tpi.CrossCutting.Models;
using System.Text.Json;

namespace Dsw2026Tpi.Api.Responses;


/// Escribe respuestas HTTP de error utilizando
/// el contrato JSON definido para toda la API.

public static class ErrorResponseWriter
{
    /*
     
Mantiene las propiedades C# en PascalCase,
pero las serializa en camelCase para respetar
el contrato HTTP.*/
    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase
        };

    
    /// Limpia la respuesta actual y escribe el error
    /// con el status HTTP y formato JSON correspondientes.
    
    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        ErrorResponse error)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(error);

        /*
         
Una respuesta que ya comenzó a enviarse no puede
reemplazarse de manera segura.*/
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