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
        modelBuilder.Entity<Entities.CategoryEntity>()
            .Property(e => e.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 100, incrementBy: 1);
        Data.Configurations.UtcDateTimeConvention.ConfigureUtcDateTimeColumnType(
            modelBuilder,
            "timestamp with time zone");
    }
}
