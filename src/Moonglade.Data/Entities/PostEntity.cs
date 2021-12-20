using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class PostEntity
{
    public PostEntity()
    {
        Comments = new HashSet<CommentEntity>();
        PostCategory = new HashSet<PostCategoryEntity>();
        Tags = new HashSet<TagEntity>();
    }

    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string Author { get; set; }
    public string PostContent { get; set; }
    public bool CommentEnabled { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public string ContentAbstract { get; set; }
    public string ContentLanguageCode { get; set; }
    public bool IsFeedIncluded { get; set; }
    public DateTime? PubDateUtc { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsOriginal { get; set; }
    public string OriginLink { get; set; }
    public string HeroImageUrl { get; set; }
    public string InlineCss { get; set; }
    public bool IsFeatured { get; set; }
    public int HashCheckSum { get; set; }

    public virtual PostExtensionEntity PostExtension { get; set; }
    public virtual ICollection<CommentEntity> Comments { get; set; }
    public virtual ICollection<PostCategoryEntity> PostCategory { get; set; }
    public virtual ICollection<TagEntity> Tags { get; set; }
}

[ExcludeFromCodeCoverage]
internal class PostConfiguration : IEntityTypeConfiguration<PostEntity>
{
    public void Configure(EntityTypeBuilder<PostEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CommentEnabled);
        builder.Property(e => e.ContentAbstract).HasMaxLength(1024);
        builder.Property(e => e.ContentLanguageCode).HasMaxLength(8);

        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.PubDateUtc).HasColumnType("datetime");
        builder.Property(e => e.LastModifiedUtc).HasColumnType("datetime");
        builder.Property(e => e.PostContent);

        builder.Property(e => e.Author).HasMaxLength(64);
        builder.Property(e => e.Slug).HasMaxLength(128);
        builder.Property(e => e.Title).HasMaxLength(128);
        builder.Property(e => e.OriginLink).HasMaxLength(256);
        builder.Property(e => e.HeroImageUrl).HasMaxLength(256);
        builder.Property(e => e.InlineCss).HasMaxLength(2048);
    }
}