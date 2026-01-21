using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

internal class CommentConfiguration : Data.Configurations.CommentConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<CommentEntity> builder)
    {
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
    }
}