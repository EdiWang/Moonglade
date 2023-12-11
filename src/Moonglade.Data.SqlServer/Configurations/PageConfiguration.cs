using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Generated.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

internal class PageConfiguration : IEntityTypeConfiguration<PageEntity>
{
    public void Configure(EntityTypeBuilder<PageEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(128);
        builder.Property(e => e.Slug).HasMaxLength(128);
        builder.Property(e => e.CssId).HasMaxLength(64);
        builder.Property(e => e.MetaDescription).HasMaxLength(256);
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.UpdateTimeUtc).HasColumnType("datetime");
    }
}