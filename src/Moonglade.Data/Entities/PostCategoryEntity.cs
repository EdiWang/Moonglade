using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class PostCategoryEntity
{
    public Guid PostId { get; set; }
    public Guid CategoryId { get; set; }

    public virtual CategoryEntity Category { get; set; }
    public virtual PostEntity Post { get; set; }
}

[ExcludeFromCodeCoverage]
internal class PostCategoryConfiguration : IEntityTypeConfiguration<PostCategoryEntity>
{
    public void Configure(EntityTypeBuilder<PostCategoryEntity> builder)
    {
        builder.HasKey(e => new { e.PostId, e.CategoryId });

        builder.HasOne(d => d.Category)
            .WithMany(p => p.PostCategory)
            .HasForeignKey(d => d.CategoryId)
            .HasConstraintName("FK_PostCategory_Category");

        builder.HasOne(d => d.Post)
            .WithMany(p => p.PostCategory)
            .HasForeignKey(d => d.PostId)
            .HasConstraintName("FK_PostCategory_Post");
    }
}