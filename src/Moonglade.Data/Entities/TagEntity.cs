using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class TagEntity
{
    public TagEntity()
    {
        Posts = new HashSet<PostEntity>();
    }

    public int Id { get; set; }
    public string DisplayName { get; set; }
    public string NormalizedName { get; set; }

    public virtual ICollection<PostEntity> Posts { get; set; }
}

[ExcludeFromCodeCoverage]
internal class TagConfiguration : IEntityTypeConfiguration<TagEntity>
{
    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.Property(e => e.DisplayName).HasMaxLength(32);
        builder.Property(e => e.NormalizedName).HasMaxLength(32);
    }
}