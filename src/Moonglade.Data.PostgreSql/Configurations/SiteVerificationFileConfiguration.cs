using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.PostgreSql.Configurations;

internal class SiteVerificationFileConfiguration : Data.Configurations.SiteVerificationFileConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<SiteVerificationFileEntity> builder)
    {
        builder.Property(e => e.CreatedTimeUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("timestamp with time zone");
    }
}
