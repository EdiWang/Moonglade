using Microsoft.EntityFrameworkCore;
using Moonglade.Data.PostgreSql.Configurations;

namespace Moonglade.Data.PostgreSql;

public class PostgreSqlBlogDbContext : BlogDbContext
{
    public PostgreSqlBlogDbContext()
    {
    }

    public PostgreSqlBlogDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BlogThemeConfiguration());
        Data.Configurations.UtcDateTimeConvention.ConfigureUtcDateTimeColumnType(
            modelBuilder,
            "timestamp with time zone");
    }
}
