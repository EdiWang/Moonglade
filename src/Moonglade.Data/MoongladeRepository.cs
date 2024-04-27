using Ardalis.Specification.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Moonglade.Data;

public class MoongladeRepository<T>(BlogDbContext dbContext) : RepositoryBase<T>(dbContext)
    where T : class
{
    public IQueryable<T> AsQueryable() => dbContext.Set<T>();

    public Task<int> CountAsync(Expression<Func<T, bool>> condition, CancellationToken ct = default) =>
        dbContext.Set<T>().CountAsync(condition, ct);

    public Task Clear(CancellationToken ct = default)
    {
        dbContext.RemoveRange(dbContext.Set<T>());
        return dbContext.SaveChangesAsync(ct);
    }

    public Task<T> GetAsync(Expression<Func<T, bool>> condition) =>
        dbContext.Set<T>().FirstOrDefaultAsync(condition);

    public async Task<List<T>> ListNoTrackingAsync(CancellationToken ct = default) =>
        await dbContext.Set<T>().AsNoTracking().ToListAsync(cancellationToken: ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> condition, CancellationToken ct = default)
    {
        return null != condition ?
            dbContext.Set<T>().AnyAsync(condition, cancellationToken: ct) :
            dbContext.Set<T>().AnyAsync(cancellationToken: ct);
    }

    public Task<TResult> FirstOrDefaultAsync<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector) =>
        ApplySpecification(spec).AsNoTracking().Select(selector).FirstOrDefaultAsync();

    public async Task<List<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct = default) =>
        await dbContext.Set<T>().AsNoTracking().Select(selector).ToListAsync(cancellationToken: ct);

    public async Task<List<TResult>> SelectAsync<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector, CancellationToken ct = default) =>
        await ApplySpecification(spec).AsNoTracking().Select(selector).ToListAsync(ct);

    public async Task<List<TResult>> SelectAsync<TGroup, TResult>(
        Expression<Func<T, TGroup>> groupExpression,
        Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
        ISpecification<T> spec = null) =>
        null != spec ?
            await ApplySpecification(spec).AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync() :
            await dbContext.Set<T>().AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync();
}