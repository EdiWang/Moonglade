using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.PostgreSql.Configurations;

internal class ActivityLogConfiguration : Data.Configurations.ActivityLogConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<ActivityLogEntity> builder)
    {
        builder.Property(e => e.EventTimeUtc).HasColumnType("timestamp");
    }
}
