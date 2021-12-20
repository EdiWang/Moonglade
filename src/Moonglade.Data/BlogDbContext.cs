using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data;

[ExcludeFromCodeCoverage]
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
    public virtual DbSet<PostExtensionEntity> PostExtension { get; set; }
    public virtual DbSet<PostTagEntity> PostTag { get; set; }
    public virtual DbSet<TagEntity> Tag { get; set; }
    public virtual DbSet<FriendLinkEntity> FriendLink { get; set; }
    public virtual DbSet<PageEntity> CustomPage { get; set; }
    public virtual DbSet<MenuEntity> Menu { get; set; }
    public virtual DbSet<SubMenuEntity> SubMenu { get; set; }
    public virtual DbSet<LocalAccountEntity> LocalAccount { get; set; }
    public virtual DbSet<AuditLogEntity> AuditLog { get; set; }
    public virtual DbSet<PingbackEntity> Pingback { get; set; }
    public virtual DbSet<BlogThemeEntity> BlogTheme { get; set; }
    public virtual DbSet<BlogAssetEntity> BlogAsset { get; set; }
    public virtual DbSet<HtmlPitchEntity> HtmlPitch { get; set; }
    public virtual DbSet<BlogConfigurationEntity> BlogConfiguration { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new CommentConfiguration());
        modelBuilder.ApplyConfiguration(new CommentReplyConfiguration());
        modelBuilder.ApplyConfiguration(new PostConfiguration());
        modelBuilder.ApplyConfiguration(new PostCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new PostExtensionConfiguration());
        modelBuilder.ApplyConfiguration(new TagConfiguration());
        modelBuilder.ApplyConfiguration(new FriendLinkConfiguration());
        modelBuilder.ApplyConfiguration(new PageConfiguration());
        modelBuilder.ApplyConfiguration(new MenuConfiguration());
        modelBuilder.ApplyConfiguration(new SubMenuConfiguration());
        modelBuilder.ApplyConfiguration(new LocalAccountConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new PingbackConfiguration());
        modelBuilder.ApplyConfiguration(new BlogThemeConfiguration());
        modelBuilder.ApplyConfiguration(new HtmlPitchConfiguration());
        modelBuilder.ApplyConfiguration(new BlogAssetConfiguration());
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