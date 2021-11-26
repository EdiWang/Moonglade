using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public interface IRepository<T> //where T : class
{
    Task ExecuteSqlRawAsync(string sql);

    ValueTask<T> GetAsync(object key);

    Task<T> GetAsync(Expression<Func<T, bool>> condition);

    Task<IReadOnlyList<T>> GetAsync();

    Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec);

    IQueryable<T> GetAsQueryable();

    TResult SelectFirstOrDefault<TResult>(
        ISpecification<T> spec,
        Expression<Func<T, TResult>> selector);

    Task DeleteAsync(T entity);

    Task DeleteAsync(IEnumerable<T> entities);

    Task DeleteAsync(object key);

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

    Task<T> AddAsync(T entity);

    Task UpdateAsync(T entity);
}