using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.MySql.Configurations;

internal class PostConfiguration : Data.Configurations.PostConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<PostEntity> builder)
    {
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.PubDateUtc).HasColumnType("datetime");
        builder.Property(e => e.LastModifiedUtc).HasColumnType("datetime");
    }
}