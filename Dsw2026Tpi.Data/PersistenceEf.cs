using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Data;

/// <summary>
/// Implementa las operaciones genéricas de persistencia
/// mediante Entity Framework Core.
/// </summary>
public class PersistenceEf : IPersistence
{
    private readonly Dsw2026TpiDbContext _context;

    public PersistenceEf(
        Dsw2026TpiDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Agrega una entidad y confirma los cambios.
    /// </summary>
    public async Task<T> Add<T>(
        T entity)
        where T : EntityBase
    {
        await _context.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    /// <summary>
    /// Elimina físicamente una entidad y confirma los cambios.
    /// </summary>
    public async Task<T> Delete<T>(
        T entity)
        where T : EntityBase
    {
        _context.Remove(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    /// <summary>
    /// Obtiene la primera entidad que cumple el predicado,
    /// o null cuando no existe ninguna coincidencia.
    /// </summary>
    public async Task<T?> First<T>(
        Expression<Func<T, bool>> predicate,
        params string[] include)
        where T : EntityBase
    {
        return await Include(
                _context.Set<T>(),
                include)
            .FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Obtiene todas las entidades del tipo solicitado.
    /// </summary>
    public async Task<IEnumerable<T>?> GetAll<T>(
        params string[] include)
        where T : EntityBase
    {
        return await Include(
                _context.Set<T>(),
                include)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene una entidad mediante su identificador.
    /// </summary>
    public async Task<T?> GetById<T>(
        Guid id,
        params string[] include)
        where T : EntityBase
    {
        return await Include(
                _context.Set<T>(),
                include)
            .FirstOrDefaultAsync(
                entity => entity.Id == id);
    }

    /// <summary>
    /// Obtiene las entidades que cumplen el predicado recibido.
    /// </summary>
    public async Task<IEnumerable<T>?> GetFiltered<T>(
        Expression<Func<T, bool>> predicate,
        params string[] include)
        where T : EntityBase
    {
        return await Include(
                _context.Set<T>(),
                include)
            .Where(predicate)
            .ToListAsync();
    }

    /// <summary>
    /// Actualiza una entidad y confirma todos los cambios
    /// pendientes en el DbContext.
    /// </summary>
    public async Task<T> Update<T>(
        T entity)
        where T : EntityBase
    {
        _context.Update(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    /// <summary>
    /// Obtiene una página ordenada de entidades que cumplen
    /// el filtro indicado.
    /// </summary>
    public async Task<Pagination<T>> Paginate<T, TKey>(
        int pageSize,
        int pageIndex,
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> sortOrder,
        params string[] includes)
        where T : EntityBase
    {
        // Normaliza valores negativos.
        pageSize = Math.Abs(pageSize);

        // Conserva el índice público comenzando desde 1.
        var originalPageIndex = Math.Abs(pageIndex);

        // Convierte el índice público a base cero para Skip().
        pageIndex = originalPageIndex == 0
            ? 0
            : originalPageIndex - 1;

        // Construye la consulta con includes, filtro y ordenamiento.
        var filtered = Include(
                _context.Set<T>(),
                includes)
            .Where(predicate)
            .OrderBy(sortOrder);

        // Cuenta todos los registros que cumplen el filtro.
        var total = await filtered.CountAsync();

        // Ejecuta la consulta correspondiente a una página.
        async Task<Pagination<T>> GetPage(
            int skip,
            int take,
            int responsePageIndex)
        {
            var data = await filtered
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return new Pagination<T>(
                pageSize,
                responsePageIndex,
                total,
                data);
        }

        // La página solicitada contiene resultados.
        if (total > pageSize * pageIndex)
        {
            return await GetPage(
                pageIndex * pageSize,
                pageSize,
                originalPageIndex);
        }

        // Todos los resultados entran en una única página.
        if (total < pageSize)
        {
            return new Pagination<T>(
                pageSize,
                originalPageIndex,
                total,
                await filtered.ToListAsync());
        }

        /*
         * Si la página solicitada excede la cantidad disponible,
         * retrocede hasta encontrar la última página con datos.
         */
        var targetPageIndex = pageIndex - 1;

        while (true)
        {
            if (total > targetPageIndex * pageSize)
            {
                return await GetPage(
                    targetPageIndex * pageSize,
                    pageSize,
                    targetPageIndex + 1);
            }

            targetPageIndex--;

            if (targetPageIndex < 0)
            {
                return new Pagination<T>(
                    pageSize,
                    originalPageIndex,
                    0,
                    []);
            }
        }
    }

    /// <summary>
    /// Agrega dinámicamente las propiedades de navegación
    /// solicitadas mediante Include.
    /// </summary>
    private static IQueryable<T> Include<T>(
        IQueryable<T> query,
        string[] includes)
        where T : EntityBase
    {
        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return query;
    }
}

/*
 * CONSIDERACIONES:
 *
 * - Delete realiza eliminación física, no soft delete.
 * - Update ejecuta SaveChanges para todas las entidades trackeadas,
 *   no solamente para la entidad recibida.
 * - GetAll y GetFiltered devuelven listas vacías, no null.
 * - Los includes como strings no tienen validación en compilación.
 * - Paginate retrocede a la última página válida cuando se solicita
 *   una página superior a la cantidad existente.
 */