using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Moonglade.Data.Infrastructure
{
    public class DbContextRepository<T> : IRepository<T> where T : class
    {
        protected readonly MoongladeDbContext DbContext;

        public DbContextRepository(MoongladeDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public T Get(object key)
        {
            return DbContext.Set<T>().Find(key);
        }

        public T Get(Expression<Func<T, bool>> condition)
        {
            return DbContext.Set<T>().FirstOrDefault(condition);
        }

        public IQueryable<T> GetAsQueryable()
        {
            return DbContext.Set<T>();
        }

        public IReadOnlyList<T> Get(bool asNoTracking = true)
        {
            return asNoTracking ?
                DbContext.Set<T>().AsNoTracking().ToList() :
                DbContext.Set<T>().ToList();
        }

        public IReadOnlyList<T> Get(ISpecification<T> spec, bool asNoTracking = true)
        {
            return asNoTracking ?
                ApplySpecification(spec).AsNoTracking().ToList() :
                ApplySpecification(spec).ToList();
        }

        public T GetFirstOrDefault(ISpecification<T> spec, bool asNoTracking = true)
        {
            return asNoTracking ?
                ApplySpecification(spec).AsNoTracking().FirstOrDefault() :
                ApplySpecification(spec).FirstOrDefault();
        }

        public IReadOnlyList<TResult> Select<TResult>(Expression<Func<T, TResult>> selector, bool asNoTracking = true)
        {
            return asNoTracking ?
                DbContext.Set<T>().AsNoTracking().Select(selector).ToList() :
                DbContext.Set<T>().Select(selector).ToList();
        }

        public TResult SelectFirstOrDefault<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector, bool asNoTracking = true)
        {
            return asNoTracking ?
                ApplySpecification(spec).AsNoTracking().Select(selector).FirstOrDefault() :
                ApplySpecification(spec).Select(selector).FirstOrDefault();
        }

        public IReadOnlyList<TResult> Select<TGroup, TResult>(
            Expression<Func<T, TGroup>> groupExpression,
            Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
            bool asNoTracking = true)
        {
            return asNoTracking ?
                DbContext.Set<T>().AsNoTracking().GroupBy(groupExpression).Select(selector).ToList() :
                DbContext.Set<T>().GroupBy(groupExpression).Select(selector).ToList();
        }

        public IReadOnlyList<TResult> Select<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector, bool asNoTracking = true)
        {
            return asNoTracking ?
                ApplySpecification(spec).AsNoTracking().Select(selector).ToList() :
                ApplySpecification(spec).Select(selector).ToList();
        }

        public T Add(T entity)
        {
            DbContext.Set<T>().Add(entity);
            DbContext.SaveChanges();

            return entity;
        }

        public int Update(T entity)
        {
            DbContext.Entry(entity).State = EntityState.Modified;
            return DbContext.SaveChanges();
        }

        public int Delete(T entity)
        {
            DbContext.Set<T>().Remove(entity);
            return DbContext.SaveChanges();
        }

        public int Delete(object key)
        {
            var entity = Get(key);
            if (null != entity)
            {
                return Delete(entity);
            }

            return -1;
        }

        public int Delete(IEnumerable<T> entities)
        {
            DbContext.Set<T>().RemoveRange(entities);
            return DbContext.SaveChanges();
        }

        public int Count(ISpecification<T> spec)
        {
            return ApplySpecification(spec).Count();
        }

        public int Count(Expression<Func<T, bool>> condition)
        {
            return DbContext.Set<T>().Count(condition);
        }

        public bool Any(Expression<Func<T, bool>> condition = null)
        {
            return null != condition ? DbContext.Set<T>().Any(condition) : DbContext.Set<T>().Any();
        }

        public virtual ValueTask<T> GetAsync(object key)
        {
            return DbContext.Set<T>().FindAsync(key);
        }

        public Task<T> GetAsync(Expression<Func<T, bool>> condition)
        {
            return DbContext.Set<T>().FirstOrDefaultAsync(condition);
        }

        public async Task<IReadOnlyList<T>> GetAsync(bool asNoTracking = true)
        {
            if (asNoTracking)
            {
                return await DbContext.Set<T>().AsNoTracking().ToListAsync();
            }
            return await DbContext.Set<T>().ToListAsync();
        }

        public async Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec, bool asNoTracking = true)
        {
            if (asNoTracking)
            {
                return await ApplySpecification(spec).AsNoTracking().ToListAsync();
            }
            return await ApplySpecification(spec).ToListAsync();
        }

        public Task<T> GetFirstOrDefaultAsync(ISpecification<T> spec, bool asNoTracking = true)
        {
            return asNoTracking ?
                ApplySpecification(spec).AsNoTracking().FirstOrDefaultAsync() :
                ApplySpecification(spec).FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, bool asNoTracking = true)
        {
            if (asNoTracking)
            {
                return await DbContext.Set<T>().AsNoTracking().Select(selector).ToListAsync();
            }
            return await DbContext.Set<T>().Select(selector).ToListAsync();
        }

        public async Task<IReadOnlyList<TResult>> SelectAsync<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector, bool asNoTracking = true)
        {
            if (asNoTracking)
            {
                return await ApplySpecification(spec).AsNoTracking().Select(selector).ToListAsync();
            }
            return await ApplySpecification(spec).Select(selector).ToListAsync();
        }

        public Task<TResult> SelectFirstOrDefaultAsync<TResult>(ISpecification<T> spec, Expression<Func<T, TResult>> selector, bool asNoTracking = true)
        {
            return asNoTracking ?
                ApplySpecification(spec).AsNoTracking().Select(selector).FirstOrDefaultAsync() :
                ApplySpecification(spec).Select(selector).FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<TResult>> SelectAsync<TGroup, TResult>(
            Expression<Func<T, TGroup>> groupExpression,
            Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
            bool asNoTracking = true)
        {
            if (asNoTracking)
            {
                return await DbContext.Set<T>().AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync();
            }
            return await DbContext.Set<T>().GroupBy(groupExpression).Select(selector).ToListAsync();
        }

        public async Task<IReadOnlyList<TResult>> SelectAsync<TGroup, TResult>(ISpecification<T> spec, Expression<Func<T, TGroup>> groupExpression, Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
            bool asNoTracking = true)
        {
            if (asNoTracking)
            {
                return await ApplySpecification(spec).AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync();
            }
            return await ApplySpecification(spec).GroupBy(groupExpression).Select(selector).ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            DbContext.Set<T>().Add(entity);
            await DbContext.SaveChangesAsync();

            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            DbContext.Entry(entity).State = EntityState.Modified;
            await DbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            DbContext.Set<T>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }

        public Task DeleteAsync(IEnumerable<T> entities)
        {
            DbContext.Set<T>().RemoveRange(entities);
            return DbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(object key)
        {
            var entity = await GetAsync(key);
            if (null != entity)
            {
                await DeleteAsync(entity);
            }
        }

        public Task<int> CountAsync(ISpecification<T> spec)
        {
            return ApplySpecification(spec).CountAsync();
        }

        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            return SpecificationEvaluator<T>.GetQuery(DbContext.Set<T>().AsQueryable(), spec);
        }
    }
}
