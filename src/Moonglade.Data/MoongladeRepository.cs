﻿using Ardalis.Specification.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Moonglade.Data;

public class MoongladeRepository<T>(BlogDbContext dbContext) : RepositoryBase<T>(dbContext)
    where T : class
{
    public IQueryable<T> AsQueryable() => dbContext.Set<T>();

    public Task Clear(CancellationToken ct = default)
    {
        dbContext.RemoveRange(dbContext.Set<T>());
        return dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(object key, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(key, ct);
        if (entity is not null) await DeleteAsync(entity, ct);
    }

    public Task<T> GetAsync(Expression<Func<T, bool>> condition) =>
        dbContext.Set<T>().FirstOrDefaultAsync(condition);

    public async Task<List<T>> ListNoTrackingAsync(CancellationToken ct = default) =>
        await dbContext.Set<T>().AsNoTracking().ToListAsync(cancellationToken: ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> condition = null, CancellationToken ct = default)
    {
        return null != condition ?
            dbContext.Set<T>().AnyAsync(condition, cancellationToken: ct) :
            dbContext.Set<T>().AnyAsync(cancellationToken: ct);
    }

    public async Task<List<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct = default) =>
        await dbContext.Set<T>().AsNoTracking().Select(selector).ToListAsync(cancellationToken: ct);
}