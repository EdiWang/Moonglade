namespace Moonglade.Data.PostgreSql.Infrastructure;

public class PostgreSqlDbContextRepository<T>(PostgreSqlBlogDbContext dbContext) : MoongladeRepository<T>(dbContext)
    where T : class;
