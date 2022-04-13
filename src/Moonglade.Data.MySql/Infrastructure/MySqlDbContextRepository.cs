using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.MySql.Infrastructure;


public class MySqlDbContextRepository<T> : DbContextRepository<T> where T : class
{
    public MySqlDbContextRepository(MySqlBlogDbContext dbContext)
        : base(dbContext)
    {
    }
}