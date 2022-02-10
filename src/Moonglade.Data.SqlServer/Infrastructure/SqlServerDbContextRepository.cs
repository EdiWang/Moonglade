using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Infrastructure;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.SqlServer.Infrastructure;

[ExcludeFromCodeCoverage]
public class SqlServerDbContextRepository<T> : DbContextRepository<T> where T : class
{
    public SqlServerDbContextRepository(SqlServerBlogDbContext dbContext)
        : base(dbContext)
    {
    }

    public override async Task ExecuteSqlRawAsync(string sql)
    {
        await DbContext.Database.ExecuteSqlRawAsync(sql);
    }
}