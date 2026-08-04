using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;

public class AuthorizationException : AppException
{
    public AuthorizationException()
        : base(
            ErrorCodes.AUTHORIZATION_FAILED,
            nameof(ErrorCodes.AUTHORIZATION_FAILED))
    {
    }
}