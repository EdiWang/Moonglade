using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        int Count(ISpecification<T> spec);
    }
}
