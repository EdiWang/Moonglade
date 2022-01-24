using Microsoft.EntityFrameworkCore;
using Moonglade.Data.SqlServer.Configurations;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.SqlServer
{
    [ExcludeFromCodeCoverage]
    public class BlogSqlServerDbContext : BlogDbContext
    {
        public BlogSqlServerDbContext()
        {
        }

        public BlogSqlServerDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new CommentConfiguration());
            modelBuilder.ApplyConfiguration(new CommentReplyConfiguration());
            modelBuilder.ApplyConfiguration(new PostConfiguration());
            modelBuilder.ApplyConfiguration(new PostCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new PostExtensionConfiguration());
            modelBuilder.ApplyConfiguration(new LocalAccountConfiguration());
            modelBuilder.ApplyConfiguration(new PingbackConfiguration());
            modelBuilder.ApplyConfiguration(new BlogThemeConfiguration());
            modelBuilder.ApplyConfiguration(new BlogAssetConfiguration());
            modelBuilder.ApplyConfiguration(new BlogConfigurationConfiguration());
            modelBuilder.ApplyConfiguration(new PageConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
