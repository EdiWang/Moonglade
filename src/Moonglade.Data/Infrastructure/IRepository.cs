using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public interface IRepository<T> where T : class
{
    Task<List<T>> ListAsync(ISpecification<T> spec);

    Task<List<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken ct = default);

    Task<List<TResult>> SelectAsync<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector, CancellationToken ct = default);
}