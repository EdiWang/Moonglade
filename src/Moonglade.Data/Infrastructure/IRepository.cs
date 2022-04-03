using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public interface IRepository<T> //where T : class
{
    Task Clear();

    ValueTask<T> GetAsync(object key);

    Task<T> GetAsync(Expression<Func<T, bool>> condition);

    Task<IReadOnlyList<T>> ListAsync();

    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);

    IQueryable<T> GetAsQueryable();

    TResult SelectFirstOrDefault<TResult>(
        ISpecification<T> spec,
        Expression<Func<T, TResult>> selector);

    Task DeleteAsync(T entity, CancellationToken ct = default);

    Task DeleteAsync(IEnumerable<T> entities, CancellationToken ct = default);

    Task DeleteAsync(object key, CancellationToken ct = default);

    int Count(ISpecification<T> spec = null);

    int Count(Expression<Func<T, bool>> condition);

    Task<int> CountAsync(ISpecification<T> spec);

    bool Any(ISpecification<T> spec);

    bool Any(Expression<Func<T, bool>> condition = null);

    Task<IReadOnlyList<TResult>> SelectAsync<TResult>(
        Expression<Func<T, TResult>> selector);

    Task<IReadOnlyList<TResult>> SelectAsync<TResult>(
        ISpecification<T> spec,
        Expression<Func<T, TResult>> selector);

    Task<TResult> SelectFirstOrDefaultAsync<TResult>(
        ISpecification<T> spec,
        Expression<Func<T, TResult>> selector);

    Task<IReadOnlyList<TResult>> SelectAsync<TGroup, TResult>(
        Expression<Func<T, TGroup>> groupExpression,
        Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
        ISpecification<T> spec = null);

    Task<T> AddAsync(T entity, CancellationToken ct = default);

    Task UpdateAsync(T entity, CancellationToken ct = default);
}