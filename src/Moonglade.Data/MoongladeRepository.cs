using Ardalis.Specification.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Moonglade.Data;

public class MoongladeRepository<T>(BlogDbContext dbContext) : RepositoryBase<T>(dbContext)
    where T : class
{
    public Task<int> CountAsync(Expression<Func<T, bool>> condition, CancellationToken ct = default) =>
        dbContext.Set<T>().CountAsync(condition, ct);

    public Task Clear(CancellationToken ct = default)
    {
        dbContext.RemoveRange(dbContext.Set<T>());
        return dbContext.SaveChangesAsync(ct);
    }

    public async Task<List<TResult>> SelectAsync<TGroup, TResult>(
        Expression<Func<T, TGroup>> groupExpression,
        Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
        ISpecification<T> spec) =>
        await ApplySpecification(spec).GroupBy(groupExpression).Select(selector).ToListAsync();
}