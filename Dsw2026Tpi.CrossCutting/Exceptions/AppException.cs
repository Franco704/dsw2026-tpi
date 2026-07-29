using Dsw2026Tpi.CrossCutting.Models;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción base para todas las excepciones de la aplicación.
/// Incluye código de error, status HTTP y datos adicionales para facilitar el manejo global.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// Código de error único para identificar el tipo de error
    /// </summary>
    public ErrorResponse Error { get; }


    /// <summary>
    /// Datos adicionales sobre el error (para debugging o información extra)
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }

    protected AppException(
        string message,
        string errorCode,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Error = new ErrorResponse(errorCode, message);
    }

    /// <summary>
    /// Agrega datos adicionales al error
    /// </summary>
    public AppException WithDetail(string field, string issue)
    {
        Error.AddDetail(field, issue);
        return this;
    }
    public AppException WithDetail(IEnumerable<(string, string)> details)
    {
        Error.AddDetail(details);
        return this;
    }
}
/*Preguntas posibles del profesor
¿Por qué crear AppException en lugar de usar solamente Exception?

Para distinguir errores controlados de errores inesperados y asociarlos a una respuesta uniforme con código, mensaje y detalles.

¿Por qué es abstracta?

Porque representa una base común y no un error concreto.

¿Qué contiene Error?

Un ErrorResponse que posteriormente será serializado por el middleware.

¿Para qué sirve innerException?

Para conservar la excepción técnica original cuando se envuelve en una excepción de aplicación.

¿Qué hace WithDetail?

Agrega información específica del error y devuelve la misma excepción para permitir encadenamiento fluido.

¿Dónde se decide el status HTTP?

En el middleware de la capa API, según el tipo concreto de excepción.

¿AppException usa directamente ErrorCodes.resx?

No. Las excepciones derivadas seleccionan el código y mensaje del recurso; AppException solo los recibe.

¿Para qué serviría AdditionalData?

Para información técnica adicional destinada a logging o debugging, aunque actualmente habría que comprobar si realmente se utiliza.

Respuesta oral posible

AppException es la clase base abstracta de los errores controlados de la aplicación. Hereda de Exception, 
conserva el mensaje y una posible excepción interna,
y además construye un ErrorResponse con un código estable y un mensaje descriptivo. 
Sus métodos WithDetail permiten agregar uno o varios detalles mediante una interfaz fluida.
El status HTTP no se almacena aquí, sino que lo determina el middleware de API según el tipo concreto de excepción, lo cual evita acoplar CrossCutting al protocolo HTTP*/
