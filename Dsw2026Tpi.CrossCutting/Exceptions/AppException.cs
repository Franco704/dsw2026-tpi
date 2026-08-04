using Dsw2026Tpi.CrossCutting.Models;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

public abstract class AppException : Exception
{
    public ErrorResponse Error { get; }


    public Dictionary<string, object>? AdditionalData { get; set; }

    protected AppException(
        string message,
        string errorCode,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Error = new ErrorResponse(errorCode, message);
    }

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

















