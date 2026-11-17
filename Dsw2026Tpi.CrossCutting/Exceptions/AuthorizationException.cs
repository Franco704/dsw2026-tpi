using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;

public class AuthorizationException : AppException
{
    public AuthorizationException()
        : base(
            ErrorCodes.AUTHORIZATION_FAILED,
            nameof(ErrorCodes.AUTHORIZATION_FAILED))
    {
    }
    public override int StatusCode => (int)HttpStatusCode.Forbidden;
}