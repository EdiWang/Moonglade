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
        modelBuilder.ApplyConfiguration(new Data.Configurations.CommentConfiguration());
        modelBuilder.ApplyConfiguration(new Data.Configurations.CommentReplyConfiguration());
        modelBuilder.ApplyConfiguration(new Data.Configurations.PostConfiguration());
        modelBuilder.ApplyConfiguration(new Data.Configurations.PostCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new Data.Configurations.MentionConfiguration());
        modelBuilder.ApplyConfiguration(new BlogThemeConfiguration());
        modelBuilder.ApplyConfiguration(new Data.Configurations.BlogAssetConfiguration());
        modelBuilder.ApplyConfiguration(new Data.Configurations.StyleSheetConfiguration());
        modelBuilder.ApplyConfiguration(new Data.Configurations.BlogConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new Data.Configurations.PageConfiguration());
        modelBuilder.ApplyConfiguration(new Data.Configurations.WidgetConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}