using Microsoft.EntityFrameworkCore;
using Moonglade.Data.SqlServer;

namespace Moonglade.Data.Infrastructure.SqlServer
{
    public class SqlServerDbContextRepository<T> : DbContextRepository<T> where T : class
    {
        public SqlServerDbContextRepository(BlogSqlServerDbContext dbContext)
            : base(dbContext)
        {
        }

        public override async Task ExecuteSqlRawAsync(string sql)
        {
            await DbContext.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
