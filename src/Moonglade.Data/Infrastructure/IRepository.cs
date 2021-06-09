using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Moonglade.Data.Infrastructure
{
    public interface IRepository<T> //where T : class
    {
        T Get(object key);

        IQueryable<T> GetAsQueryable();

        TResult SelectFirstOrDefault<TResult>(
            ISpecification<T> spec, 
            Expression<Func<T, TResult>> selector,
            bool asNoTracking = true);

        void Delete(T entity);

        int Delete(IEnumerable<T> entities);

        Task DeleteAsync(T entity);

        Task DeleteAsync(IEnumerable<T> entities);

        Task DeleteAsync(object key);

        int Count(ISpecification<T> spec = null);

        int Count(Expression<Func<T, bool>> condition);

        bool Any(ISpecification<T> spec);

        bool Any(Expression<Func<T, bool>> condition = null);

        ValueTask<T> GetAsync(object key);

        Task<T> GetAsync(Expression<Func<T, bool>> condition);

        Task<IReadOnlyList<T>> GetAsync();

        Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec);

        Task<T> GetFirstOrDefaultAsync(ISpecification<T> spec, bool asNoTracking = true);

        Task<IReadOnlyList<TResult>> SelectAsync<TResult>(
            Expression<Func<T, TResult>> selector);

        Task<IReadOnlyList<TResult>> SelectAsync<TResult>(
            ISpecification<T> spec,
            Expression<Func<T, TResult>> selector);

        Task<TResult> SelectFirstOrDefaultAsync<TResult>(
            ISpecification<T> spec,
            Expression<Func<T, TResult>> selector,
            bool asNoTracking = true);

        Task<IReadOnlyList<TResult>> SelectAsync<TGroup, TResult>(
            Expression<Func<T, TGroup>> groupExpression,
            Expression<Func<IGrouping<TGroup, T>, TResult>> selector);

        Task<IReadOnlyList<TResult>> SelectAsync<TGroup, TResult>(
            ISpecification<T> spec,
            Expression<Func<T, TGroup>> groupExpression,
            Expression<Func<IGrouping<TGroup, T>, TResult>> selector);

        Task<T> AddAsync(T entity);

        Task UpdateAsync(T entity);

        Task<int> CountAsync(ISpecification<T> spec);
    }
}