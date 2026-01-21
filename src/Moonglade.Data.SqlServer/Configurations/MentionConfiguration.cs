using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

internal class MentionConfiguration : Data.Configurations.MentionConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<MentionEntity> builder)
    {
        builder.Property(e => e.PingTimeUtc).HasColumnType("datetime");
    }
}