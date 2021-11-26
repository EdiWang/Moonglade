using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Configurations;

[ExcludeFromCodeCoverage]
internal class MenuConfiguration : IEntityTypeConfiguration<MenuEntity>
{
    public void Configure(EntityTypeBuilder<MenuEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(64);
        builder.Property(e => e.Url).HasMaxLength(256);
        builder.Property(e => e.Icon).HasMaxLength(64);
    }
}