using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public abstract class DbContextRepository<T> : IRepository<T> where T : class
{
    protected readonly DbContext DbContext;

    protected DbContextRepository(DbContext ctx) => DbContext = ctx;

    public Task Clear(CancellationToken ct = default)
    {
        DbContext.RemoveRange(DbContext.Set<T>());
        return DbContext.SaveChangesAsync(ct);
    }

    public Task<T> GetAsync(Expression<Func<T, bool>> condition) =>
        DbContext.Set<T>().FirstOrDefaultAsync(condition);

    public virtual ValueTask<T> GetAsync(object key, CancellationToken ct = default) =>
        DbContext.Set<T>().FindAsync(keyValues: new[] { key }, cancellationToken: ct);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
        await DbContext.Set<T>().AsNoTracking().ToListAsync(cancellationToken: ct);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec) =>
        await ApplySpecification(spec).AsNoTracking().ToListAsync();

    public IQueryable<T> GetAsQueryable() => DbContext.Set<T>();

    public TResult SelectFirstOrDefault<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector) =>
        ApplySpecification(spec).AsNoTracking().Select(selector).FirstOrDefault();

    public async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        DbContext.Set<T>().Remove(entity);
        await DbContext.SaveChangesAsync(ct);
    }

    public Task DeleteAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        DbContext.Set<T>().RemoveRange(entities);
        return DbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(object key, CancellationToken ct = default)
    {
        var entity = await GetAsync(key, ct);
        if (entity is not null) await DeleteAsync(entity, ct);
    }

    public int Count(ISpecification<T> spec = null) =>
        null != spec ?
            ApplySpecification(spec).Count() :
            DbContext.Set<T>().Count();

    public int Count(Expression<Func<T, bool>> condition) => DbContext.Set<T>().Count(condition);
    public Task<int> CountAsync(ISpecification<T> spec) => ApplySpecification(spec).CountAsync();

    public Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default) =>
        ApplySpecification(spec).AnyAsync(cancellationToken: ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> condition = null, CancellationToken ct = default) =>
        null != condition ?
            DbContext.Set<T>().AnyAsync(condition, cancellationToken: ct) :
            DbContext.Set<T>().AnyAsync(cancellationToken: ct);

    public async Task<IReadOnlyList<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector) =>
        await DbContext.Set<T>().AsNoTracking().Select(selector).ToListAsync();

    public async Task<IReadOnlyList<TResult>> SelectAsync<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector) =>
        await ApplySpecification(spec).AsNoTracking().Select(selector).ToListAsync();

    public Task<TResult> SelectFirstOrDefaultAsync<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector) =>
        ApplySpecification(spec).AsNoTracking().Select(selector).FirstOrDefaultAsync();

    public async Task<IReadOnlyList<TResult>> SelectAsync<TGroup, TResult>(
        Expression<Func<T, TGroup>> groupExpression,
        Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
        ISpecification<T> spec = null) =>
        null != spec ?
            await ApplySpecification(spec).AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync() :
            await DbContext.Set<T>().AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync();

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await DbContext.Set<T>().AddAsync(entity, ct);
        await DbContext.SaveChangesAsync(ct);

        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        DbContext.Entry(entity).State = EntityState.Modified;
        await DbContext.SaveChangesAsync(ct);
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> spec) =>
        SpecificationEvaluator<T>.GetQuery(DbContext.Set<T>().AsQueryable(), spec);
}