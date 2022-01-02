using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.MySql.Infrastructure
{
    public class MySqlDbContextRepository<T> : DbContextRepository<T> where T : class
    {
        public MySqlDbContextRepository(BlogMySqlDbContext dbContext)
            : base(dbContext)
        {
        }

        public override async Task ExecuteSqlRawAsync(string sql)
        {
            await DbContext.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
