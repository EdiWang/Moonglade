using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public abstract class DbContextRepository<T>(DbContext ctx) : IRepository<T>
    where T : class
{
    protected readonly DbContext DbContext = ctx;

    public async Task<List<TResult>> SelectAsync<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector, CancellationToken ct = default) =>
        await ApplySpecification(spec).AsNoTracking().Select(selector).ToListAsync(ct);

    private IQueryable<T> ApplySpecification(ISpecification<T> spec) =>
        SpecificationEvaluator<T>.GetQuery(DbContext.Set<T>().AsQueryable(), spec);
}