namespace Dsw2026Tpi.CrossCutting.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message, string errorCode)
        : base(message, errorCode)
    {

    }
}
