using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public interface IRepository<T> where T : class
{
    Task Clear(CancellationToken ct = default);

    ValueTask<T> GetAsync(object key, CancellationToken ct = default);

    Task<T> GetAsync(Expression<Func<T, bool>> condition);

    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);

    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);

    IQueryable<T> AsQueryable();

    Task DeleteAsync(T entity, CancellationToken ct = default);

    Task DeleteAsync(IEnumerable<T> entities, CancellationToken ct = default);

    Task DeleteAsync(object key, CancellationToken ct = default);

    Task<int> CountAsync(Expression<Func<T, bool>> condition, CancellationToken ct = default);

    Task<int> CountAsync(ISpecification<T> spec = null, CancellationToken ct = default);

    Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> condition = null, CancellationToken ct = default);

    Task<IReadOnlyList<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct = default);

    Task<IReadOnlyList<TResult>> SelectAsync<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector, CancellationToken ct = default);

    Task<TResult> FirstOrDefaultAsync<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector);

    Task<IReadOnlyList<TResult>> SelectAsync<TGroup, TResult>(
        Expression<Func<T, TGroup>> groupExpression,
        Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
        ISpecification<T> spec = null);

    Task<T> AddAsync(T entity, CancellationToken ct = default);

    Task UpdateAsync(T entity, CancellationToken ct = default);
}