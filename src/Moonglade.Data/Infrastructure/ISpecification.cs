using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Moonglade.Data.Infrastructure;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    Func<IQueryable<T>, IIncludableQueryable<T, object>> Include { get; }
    Expression<Func<T, object>> OrderBy { get; }
    Expression<Func<T, object>> OrderByDescending { get; }

    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }
}