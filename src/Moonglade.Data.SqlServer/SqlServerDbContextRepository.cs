using Ardalis.Specification.EntityFrameworkCore;

namespace Moonglade.Data.SqlServer;


public class SqlServerDbContextRepository<T>(SqlServerBlogDbContext dbContext) : RepositoryBase<T>(dbContext)
    where T : class;