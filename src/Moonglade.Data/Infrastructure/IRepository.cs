using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Data.Infrastructure
{
    public interface IRepository<T> where T : class
    {
        T Get(object key);

        IReadOnlyList<T> Get(bool asNoTracking = true);

        IReadOnlyList<T> Get(ISpecification<T> spec, bool asNoTracking = true);

        T Add(T entity);

        int Update(T entity);

        int Delete(T entity);

        int Delete(object key);

        int Count(ISpecification<T> spec);

        Task<T> GetAsync(object key);

        Task<IReadOnlyList<T>> GetAsync(bool asNoTracking = true);

        Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec, bool asNoTracking = true);

        Task<T> AddAsync(T entity);

        Task UpdateAsync(T entity);

        Task DeleteAsync(T entity);

        Task DeleteAsync(object key);

        Task<int> CountAsync(ISpecification<T> spec);
    }
}
