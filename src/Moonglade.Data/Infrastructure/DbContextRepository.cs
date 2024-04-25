using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public abstract class DbContextRepository<T>(DbContext ctx) : IRepository<T>
    where T : class
{
    protected readonly DbContext DbContext = ctx;

    public Task<T> GetAsync(Expression<Func<T, bool>> condition) =>
        DbContext.Set<T>().FirstOrDefaultAsync(condition);

    public async Task<List<T>> ListAsync(ISpecification<T> spec) =>
        await ApplySpecification(spec).AsNoTracking().ToListAsync();

    public Task<bool> AnyAsync(Expression<Func<T, bool>> condition = null, CancellationToken ct = default) =>
        null != condition ?
            DbContext.Set<T>().AnyAsync(condition, cancellationToken: ct) :
            DbContext.Set<T>().AnyAsync(cancellationToken: ct);

    public async Task<List<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct = default) =>
        await DbContext.Set<T>().AsNoTracking().Select(selector).ToListAsync(cancellationToken: ct);

    public async Task<List<TResult>> SelectAsync<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector, CancellationToken ct = default) =>
        await ApplySpecification(spec).AsNoTracking().Select(selector).ToListAsync(ct);

    public Task<TResult> FirstOrDefaultAsync<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector) =>
        ApplySpecification(spec).AsNoTracking().Select(selector).FirstOrDefaultAsync();

    public async Task<List<TResult>> SelectAsync<TGroup, TResult>(
        Expression<Func<T, TGroup>> groupExpression,
        Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
        ISpecification<T> spec = null) =>
        null != spec ?
            await ApplySpecification(spec).AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync() :
            await DbContext.Set<T>().AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync();

    private IQueryable<T> ApplySpecification(ISpecification<T> spec) =>
        SpecificationEvaluator<T>.GetQuery(DbContext.Set<T>().AsQueryable(), spec);
}