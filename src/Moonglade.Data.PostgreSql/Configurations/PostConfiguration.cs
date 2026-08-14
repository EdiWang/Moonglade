using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.PostgreSql.Configurations;

internal class PostConfiguration : Data.Configurations.PostConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<PostEntity> builder)
    {
        builder.Property(e => e.CreateTimeUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.PubDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.LastModifiedUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.ScheduledPublishTimeUtc).HasColumnType("timestamp with time zone");
    }
}
