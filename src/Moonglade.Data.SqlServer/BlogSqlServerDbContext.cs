using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Moonglade.Data.SqlServer.Configurations;

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
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
