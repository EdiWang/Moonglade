using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.PostgreSql.Configurations;

internal class StyleSheetConfiguration : Data.Configurations.StyleSheetConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<StyleSheetEntity> builder)
    {
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("timestamp");
    }
}