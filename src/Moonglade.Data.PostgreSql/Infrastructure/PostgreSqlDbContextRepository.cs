using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.PostgreSql.Infrastructure;

public class PostgreSqlDbContextRepository<T> : DbContextRepository<T> where T : class
{
    public PostgreSqlDbContextRepository(PostgreSqlBlogDbContext dbContext)
        : base(dbContext)
    {
    }
}
