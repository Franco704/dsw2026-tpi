namespace Dsw2026Tpi.CrossCutting.Exceptions;

public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string errorCode)
        : base(message, errorCode)
    {
    }
}
