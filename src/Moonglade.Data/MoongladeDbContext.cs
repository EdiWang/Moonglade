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

        public virtual DbSet<CategoryEntity> Category { get; set; }
        public virtual DbSet<CommentEntity> Comment { get; set; }
        public virtual DbSet<CommentReplyEntity> CommentReply { get; set; }
        public virtual DbSet<PingbackHistoryEntity> PingbackHistory { get; set; }
        public virtual DbSet<PostEntity> Post { get; set; }
        public virtual DbSet<PostCategoryEntity> PostCategory { get; set; }
        public virtual DbSet<PostExtensionEntity> PostExtension { get; set; }
        public virtual DbSet<PostPublishEntity> PostPublish { get; set; }
        public virtual DbSet<PostTagEntity> PostTag { get; set; }
        public virtual DbSet<TagEntity> Tag { get; set; }
        public virtual DbSet<FriendLinkEntity> FriendLink { get; set; }
        public virtual DbSet<CustomPageEntity> CustomPage { get; set; }
        public virtual DbSet<MenuEntity> Menu { get; set; }

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
            modelBuilder.ApplyConfiguration(new CustomPageConfiguration());
            modelBuilder.ApplyConfiguration(new MenuConfiguration());
        }
    }
}
