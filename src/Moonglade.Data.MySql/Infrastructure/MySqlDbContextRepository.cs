using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.MySql.Infrastructure;

[ExcludeFromCodeCoverage]
public class MySqlDbContextRepository<T> : DbContextRepository<T> where T : class
{
    public MySqlDbContextRepository(MySqlBlogDbContext dbContext)
        : base(dbContext)
    {
    }

    public override async Task ExecuteSqlRawAsync(string sql)
    {
        await DbContext.Database.ExecuteSqlRawAsync(sql);
    }
}