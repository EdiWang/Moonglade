using Ardalis.Specification.EntityFrameworkCore;

namespace Moonglade.Data.PostgreSql.Infrastructure;

public class PostgreSqlDbContextRepository<T>(PostgreSqlBlogDbContext dbContext) : RepositoryBase<T>(dbContext)
    where T : class;
