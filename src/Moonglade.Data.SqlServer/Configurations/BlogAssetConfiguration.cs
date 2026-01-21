using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

internal class BlogAssetConfiguration : Data.Configurations.BlogAssetConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<BlogAssetEntity> builder)
    {
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}