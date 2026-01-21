using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.MySql.Configurations;

internal class CommentReplyConfiguration : Data.Configurations.CommentReplyConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<CommentReplyEntity> builder)
    {
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
    }
}