using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

public class EntityNotFoundException : AppException
{
    public EntityNotFoundException(string entityName)
        : base(string.Format(ErrorCodes.ENTITY_NOTFOUND, entityName),
            nameof(ErrorCodes.ENTITY_NOTFOUND))
    {
    }
}
