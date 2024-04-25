namespace Moonglade.Data.MySql.Infrastructure;


public class MySqlDbContextRepository<T>(MySqlBlogDbContext dbContext) : MoongladeRepository<T>(dbContext)
    where T : class;