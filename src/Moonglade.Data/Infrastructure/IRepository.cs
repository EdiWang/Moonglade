using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public interface IRepository<T> where T : class
{
    ValueTask<T> GetAsync(object key, CancellationToken ct = default);

    Task<T> GetAsync(Expression<Func<T, bool>> condition);

    Task<List<T>> ListAsync(ISpecification<T> spec);

    Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> condition = null, CancellationToken ct = default);

    Task<List<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct = default);

    Task<List<TResult>> SelectAsync<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector, CancellationToken ct = default);

    Task<TResult> FirstOrDefaultAsync<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector);

    Task<List<TResult>> SelectAsync<TGroup, TResult>(
        Expression<Func<T, TGroup>> groupExpression,
        Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
        ISpecification<T> spec = null);

    Task<T> AddAsync(T entity, CancellationToken ct = default);

    Task UpdateAsync(T entity, CancellationToken ct = default);
}