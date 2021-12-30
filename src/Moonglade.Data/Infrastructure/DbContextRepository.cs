using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public abstract class DbContextRepository<T> : IRepository<T> where T : class
{
    protected readonly DbContext DbContext;

    public DbContextRepository(DbContext dbContext)
    {
        DbContext = dbContext;
    }

    public abstract Task ExecuteSqlRawAsync(string sql);

    public Task<T> GetAsync(Expression<Func<T, bool>> condition)
    {
        return DbContext.Set<T>().FirstOrDefaultAsync(condition);
    }

    public virtual ValueTask<T> GetAsync(object key)
    {
        return DbContext.Set<T>().FindAsync(key);
    }

    public async Task<IReadOnlyList<T>> GetAsync()
    {
        return await DbContext.Set<T>().AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).AsNoTracking().ToListAsync();
    }

    public IQueryable<T> GetAsQueryable()
    {
        return DbContext.Set<T>();
    }

    public TResult SelectFirstOrDefault<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector)
    {
        return ApplySpecification(spec).AsNoTracking().Select(selector).FirstOrDefault();
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
        if (entity is not null) await DeleteAsync(entity);
    }

    public int Count(ISpecification<T> spec = null)
    {
        return null != spec ? ApplySpecification(spec).Count() : DbContext.Set<T>().Count();
    }

    public int Count(Expression<Func<T, bool>> condition)
    {
        return DbContext.Set<T>().Count(condition);
    }

    public bool Any(ISpecification<T> spec)
    {
        return ApplySpecification(spec).Any();
    }

    public bool Any(Expression<Func<T, bool>> condition = null)
    {
        return null != condition ? DbContext.Set<T>().Any(condition) : DbContext.Set<T>().Any();
    }

    public async Task<IReadOnlyList<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector)
    {
        return await DbContext.Set<T>().AsNoTracking().Select(selector).ToListAsync();
    }

    public async Task<IReadOnlyList<TResult>> SelectAsync<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector)
    {
        return await ApplySpecification(spec).AsNoTracking().Select(selector).ToListAsync();
    }

    public Task<TResult> SelectFirstOrDefaultAsync<TResult>(
        ISpecification<T> spec, Expression<Func<T, TResult>> selector)
    {
        return ApplySpecification(spec).AsNoTracking().Select(selector).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<TResult>> SelectAsync<TGroup, TResult>(
        Expression<Func<T, TGroup>> groupExpression,
        Expression<Func<IGrouping<TGroup, T>, TResult>> selector,
        ISpecification<T> spec = null)
    {
        return null != spec ?
            await ApplySpecification(spec).AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync() :
            await DbContext.Set<T>().AsNoTracking().GroupBy(groupExpression).Select(selector).ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        await DbContext.Set<T>().AddAsync(entity);
        await DbContext.SaveChangesAsync();

        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        DbContext.Entry(entity).State = EntityState.Modified;
        await DbContext.SaveChangesAsync();
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