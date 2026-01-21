using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

internal class BlogConfigurationConfiguration : Data.Configurations.BlogConfigurationConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<BlogConfigurationEntity> builder)
    {
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}