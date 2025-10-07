using Ardalis.Specification.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Moonglade.Data;

public class MoongladeRepository<T>(BlogDbContext dbContext) : RepositoryBase<T>(dbContext)
    where T : class
{
    public Task<int> Clear(CancellationToken ct = default) => dbContext.Set<T>().ExecuteDeleteAsync(ct);

    public async Task<List<TResult>> SelectAsync<TGroup, TResult>(
        Expression<Func<T, TGroup>> groupExpression,
        Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
        ISpecification<T> spec) =>
        await ApplySpecification(spec).GroupBy(groupExpression).Select(selector).ToListAsync();
}