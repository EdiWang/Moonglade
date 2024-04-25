namespace Moonglade.Data.SqlServer.Infrastructure;


public class SqlServerDbContextRepository<T>(SqlServerBlogDbContext dbContext) : MoongladeRepository<T>(dbContext)
    where T : class;