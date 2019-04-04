using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Data.Infrastructure
{
    public class DbContextRepository<T> : IRepository<T>, IAsyncRepository<T> where T : class
    {
        public T Get(object key)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<T> Get()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<T> Get(ISpecification<T> spec)
        {
            throw new NotImplementedException();
        }

        public T Add(T entity)
        {
            throw new NotImplementedException();
        }

        public int Update(T entity)
        {
            throw new NotImplementedException();
        }

        public int Delete(T entity)
        {
            throw new NotImplementedException();
        }

        public int Count(ISpecification<T> spec)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetAsync(object key)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<T>> GetAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec)
        {
            throw new NotImplementedException();
        }

        public Task<T> AddAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task<int> CountAsync(ISpecification<T> spec)
        {
            throw new NotImplementedException();
        }
    }
}
