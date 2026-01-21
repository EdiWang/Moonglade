using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class PageConfiguration : IEntityTypeConfiguration<PageEntity>
{
    public void Configure(EntityTypeBuilder<PageEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(128);
        builder.Property(e => e.Slug).HasMaxLength(128);
        builder.Property(e => e.CssId).HasMaxLength(64);
        builder.Property(e => e.MetaDescription).HasMaxLength(256);
        builder.Property(e => e.HtmlContent).IsRequired();
        builder.Property(e => e.CssId);
        builder.Property(e => e.HtmlContent);
        ConfigureDateTimeColumns(builder);
    }

    protected virtual void ConfigureDateTimeColumns(EntityTypeBuilder<PageEntity> builder)
    {
        // Default: use datetime (SQL Server/MySQL compatible)
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.UpdateTimeUtc).HasColumnType("datetime");
    }
}
