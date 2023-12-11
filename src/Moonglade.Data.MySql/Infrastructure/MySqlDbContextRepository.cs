using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.MySql.Infrastructure;


public class MySqlDbContextRepository<T>(MySqlBlogDbContext dbContext) : DbContextRepository<T>(dbContext)
    where T : class;