using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class StyleSheetConfiguration : IEntityTypeConfiguration<StyleSheetEntity>
{
    public void Configure(EntityTypeBuilder<StyleSheetEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CssContent).IsRequired();
        builder.Property(e => e.FriendlyName).HasMaxLength(32);
        builder.Property(e => e.Hash).HasMaxLength(64);
        ConfigureDateTimeColumns(builder);
    }

    protected virtual void ConfigureDateTimeColumns(EntityTypeBuilder<StyleSheetEntity> builder)
    {
        // Default: use datetime (SQL Server/MySQL compatible)
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}
