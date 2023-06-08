using Moonglade.Data.Entities;

namespace Moonglade.Data;

public class BlogDbContext : DbContext
{
    public BlogDbContext()
    {
    }

    public BlogDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public virtual DbSet<CategoryEntity> Category { get; set; }
    public virtual DbSet<CommentEntity> Comment { get; set; }
    public virtual DbSet<CommentReplyEntity> CommentReply { get; set; }
    public virtual DbSet<PostEntity> Post { get; set; }
    public virtual DbSet<PostCategoryEntity> PostCategory { get; set; }
    public virtual DbSet<PostTagEntity> PostTag { get; set; }
    public virtual DbSet<TagEntity> Tag { get; set; }
    public virtual DbSet<FriendLinkEntity> FriendLink { get; set; }
    public virtual DbSet<PageEntity> CustomPage { get; set; }
    public virtual DbSet<LocalAccountEntity> LocalAccount { get; set; }
    public virtual DbSet<PingbackEntity> Pingback { get; set; }
    public virtual DbSet<BlogThemeEntity> BlogTheme { get; set; }
    public virtual DbSet<BlogAssetEntity> BlogAsset { get; set; }
    public virtual DbSet<BlogConfigurationEntity> BlogConfiguration { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new TagConfiguration());
        modelBuilder.ApplyConfiguration(new FriendLinkConfiguration());
        modelBuilder.ApplyConfiguration(new BlogConfigurationConfiguration());

        modelBuilder
            .Entity<PostEntity>()
            .HasMany(p => p.Tags)
            .WithMany(p => p.Posts)
            .UsingEntity<PostTagEntity>(
                j => j
                    .HasOne(pt => pt.Tag)
                    .WithMany()
                    .HasForeignKey(pt => pt.TagId),
                j => j
                    .HasOne(pt => pt.Post)
                    .WithMany()
                    .HasForeignKey(pt => pt.PostId));
    }
}

public static class BlogDbContextExtension
{
    public static async Task ClearAllData(this BlogDbContext context)
    {
        context.PostTag.RemoveRange();
        context.PostCategory.RemoveRange();
        context.CommentReply.RemoveRange();
        context.Category.RemoveRange();
        context.Tag.RemoveRange();
        context.Comment.RemoveRange();
        context.FriendLink.RemoveRange();
        context.Pingback.RemoveRange();
        context.Post.RemoveRange();
        context.BlogConfiguration.RemoveRange();
        context.BlogAsset.RemoveRange();
        context.BlogTheme.RemoveRange();
        context.LocalAccount.RemoveRange();

        await context.SaveChangesAsync();
    }
}