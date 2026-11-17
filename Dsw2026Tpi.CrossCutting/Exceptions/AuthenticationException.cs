using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

public class AuthenticationException : AppException
{
    public AuthenticationException()
        : base(
            ErrorCodes.AUTHENTICATION_FAILED,
            nameof(ErrorCodes.AUTHENTICATION_FAILED))
    {
    }
    public override int StatusCode => (int)HttpStatusCode.Unauthorized;
}
