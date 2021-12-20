using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class FriendLinkEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public string LinkUrl { get; set; }
}

[ExcludeFromCodeCoverage]
internal class FriendLinkConfiguration : IEntityTypeConfiguration<FriendLinkEntity>
{
    public void Configure(EntityTypeBuilder<FriendLinkEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(64);
        builder.Property(e => e.LinkUrl).HasMaxLength(256);
    }
}