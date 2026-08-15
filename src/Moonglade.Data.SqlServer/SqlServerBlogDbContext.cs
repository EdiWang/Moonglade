using Microsoft.EntityFrameworkCore;
using Moonglade.Data.SqlServer.Configurations;

namespace Moonglade.Data.SqlServer;


public class SqlServerBlogDbContext : BlogDbContext
{
    public SqlServerBlogDbContext()
    {
    }

    public SqlServerBlogDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BlogThemeConfiguration());
        Data.Configurations.UtcDateTimeConvention.ConfigureUtcDateTimeColumnType(modelBuilder, "datetime2(7)");
    }
}
