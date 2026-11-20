using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Data;

public class PersistenceEf : IPersistence
{
    private readonly Dsw2026TpiDbContext _context;

    public PersistenceEf(
        Dsw2026TpiDbContext context)
    {
        _context = context;
    }

    public async Task<T> Add<T>(
        T entity)
        where T : EntityBase
    {
        await _context.AddAsync(entity);

        return entity;
    }

    public Task<T> Delete<T>(
        T entity)
        where T : EntityBase
    {
        _context.Remove(entity);

        return Task.FromResult(entity);
    }

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

    public async Task<IEnumerable<T>?> GetAll<T>(
        params string[] include)
        where T : EntityBase
    {
        return await Include(
                _context.Set<T>(),
                include)
            .ToListAsync();
    }

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

    public Task<T> Update<T>(
        T entity)
        where T : EntityBase
    {
        _context.Update(entity);

        return Task.FromResult(entity);
    }

    public Task<Pagination<T>> Paginate<T, TKey>(
        int pageSize,
        int pageIndex,
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> sortOrder,
        params string[] includes)
        where T : EntityBase =>
        Paginate(pageSize, pageIndex, predicate, sortOrder, false, includes);

    public async Task<Pagination<T>> Paginate<T, TKey>(
        int pageSize,
        int pageIndex,
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> sortOrder,
        bool descending,
        params string[] includes)
        where T : EntityBase
    {
        pageSize = Math.Abs(pageSize);

        var originalPageIndex = Math.Abs(pageIndex);

        pageIndex = originalPageIndex == 0
            ? 0
            : originalPageIndex - 1;

        var filtered = Include(
                _context.Set<T>(),
                includes)
            .Where(predicate);

        var ordered = descending
            ? filtered.OrderByDescending(sortOrder)
            : filtered.OrderBy(sortOrder);

        var total = await ordered.CountAsync();

        async Task<Pagination<T>> GetPage(
            int skip,
            int take,
            int responsePageIndex)
        {
            var data = await ordered
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return new Pagination<T>(
                pageSize,
                responsePageIndex,
                total,
                data);
        }

        if (total > pageSize * pageIndex)
        {
            return await GetPage(
                pageIndex * pageSize,
                pageSize,
                originalPageIndex);
        }

        if (total < pageSize)
        {
            return new Pagination<T>(
                pageSize,
                originalPageIndex,
                total,
                await filtered.ToListAsync());
        }

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
    
    public Task<int> Count<T>(
        Expression<Func<T, bool>> predicate)
        where T : EntityBase =>
        _context.Set<T>().CountAsync(predicate);

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

