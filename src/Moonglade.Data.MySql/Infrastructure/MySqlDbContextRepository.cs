using Ardalis.Specification.EntityFrameworkCore;

namespace Moonglade.Data.MySql.Infrastructure;


public class MySqlDbContextRepository<T>(MySqlBlogDbContext dbContext) : RepositoryBase<T>(dbContext)
    where T : class;