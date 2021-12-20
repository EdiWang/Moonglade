using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class PostExtensionEntity
{
    public Guid PostId { get; set; }
    public int Hits { get; set; }
    public int Likes { get; set; }

    public virtual PostEntity Post { get; set; }
}

[ExcludeFromCodeCoverage]
internal class PostExtensionConfiguration : IEntityTypeConfiguration<PostExtensionEntity>
{
    public void Configure(EntityTypeBuilder<PostExtensionEntity> builder)
    {
        builder.HasKey(e => e.PostId);
        builder.Property(e => e.PostId).ValueGeneratedNever();

        builder.HasOne(d => d.Post)
            .WithOne(p => p.PostExtension)
            .HasForeignKey<PostExtensionEntity>(d => d.PostId)
            .HasConstraintName("FK_PostExtension_Post");
    }
}