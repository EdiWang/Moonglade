using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.SqlServer.Infrastructure;


public class SqlServerDbContextRepository<T> : DbContextRepository<T> where T : class
{
    public SqlServerDbContextRepository(SqlServerBlogDbContext dbContext)
        : base(dbContext)
    {
    }
}