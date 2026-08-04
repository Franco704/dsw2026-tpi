using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

public class AuthenticationException : AppException
{
    public AuthenticationException()
        : base(
            ErrorCodes.AUTHENTICATION_FAILED,
            nameof(ErrorCodes.AUTHENTICATION_FAILED))
    {
    }
}
