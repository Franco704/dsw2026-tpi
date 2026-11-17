using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

public class ValidationException : AppException
{
    public ValidationException()
        : base(
            ErrorCodes.VALIDATION_ERROR,
            nameof(ErrorCodes.VALIDATION_ERROR))
    {
    }

    public ValidationException(string message, string errorCode)
        : base(message, errorCode)
    {
    }
    
    public override int StatusCode => (int)HttpStatusCode.BadRequest;
    
    public void ThrowIfAny()
    {
        if (Error.Details.Any())
        {
            throw this;
        }
    }
}