namespace Dsw2026Tpi.CrossCutting.Exceptions;
using System.Net;

public class ConflictException : AppException
{
    public ConflictException(string message, string errorCode)
        : base(message, errorCode)
    {

    }
    public override int StatusCode => (int)HttpStatusCode.Conflict;
}
