using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Configurations;
using Moonglade.Data.Entities;

namespace Moonglade.Data
{
    public class MoongladeDbContext : DbContext
    {
        public MoongladeDbContext()
        {
        }

        public MoongladeDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Category> Category { get; set; }
        public virtual DbSet<Comment> Comment { get; set; }
        public virtual DbSet<CommentReply> CommentReply { get; set; }
        public virtual DbSet<PingbackHistory> PingbackHistory { get; set; }
        public virtual DbSet<Post> Post { get; set; }
        public virtual DbSet<PostCategory> PostCategory { get; set; }
        public virtual DbSet<PostExtension> PostExtension { get; set; }
        public virtual DbSet<PostPublish> PostPublish { get; set; }
        public virtual DbSet<PostTag> PostTag { get; set; }
        public virtual DbSet<Tag> Tag { get; set; }
        public virtual DbSet<FriendLink> FriendLink { get; set; }
        public virtual DbSet<BlogConfiguration> BlogConfiguration { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new CommentConfiguration());
            modelBuilder.ApplyConfiguration(new CommentReplyConfiguration());
            modelBuilder.ApplyConfiguration(new PingbackHistoryConfiguration());
            modelBuilder.ApplyConfiguration(new PostConfiguration());
            modelBuilder.ApplyConfiguration(new PostCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new PostExtensionConfiguration());
            modelBuilder.ApplyConfiguration(new PostPublishConfiguration());
            modelBuilder.ApplyConfiguration(new PostTagConfiguration());
            modelBuilder.ApplyConfiguration(new TagConfiguration());
            modelBuilder.ApplyConfiguration(new FriendLinkConfiguration());
            modelBuilder.ApplyConfiguration(new BlogConfigurationConfiguration());
        }
    }
}
