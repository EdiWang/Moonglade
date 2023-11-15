using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.SqlServer.Infrastructure;


public class SqlServerDbContextRepository<T>(SqlServerBlogDbContext dbContext) : DbContextRepository<T>(dbContext)
    where T : class;