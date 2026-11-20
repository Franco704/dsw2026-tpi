using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Extensions;

public static class PersistenceExtensions
{
    public static async Task<T> GetRequiredById<T>(
        this IPersistence persistence,
        Guid id,
        params string[] include)
        where T : EntityBase =>
        await persistence.GetById<T>(id, include)
        ?? throw new EntityNotFoundException(typeof(T).Name);
}
