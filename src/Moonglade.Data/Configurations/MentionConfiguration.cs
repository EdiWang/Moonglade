using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class MentionConfiguration : IEntityTypeConfiguration<MentionEntity>
{
    public void Configure(EntityTypeBuilder<MentionEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.TargetPostId);
        builder.Property(e => e.TargetPostTitle).HasMaxLength(256);
        builder.Property(e => e.Domain).HasMaxLength(256);
        builder.Property(e => e.SourceIp).HasMaxLength(64);
        builder.Property(e => e.SourceTitle).HasMaxLength(256);
        builder.Property(e => e.SourceUrl).HasMaxLength(256);
        builder.Property(e => e.Worker).HasMaxLength(16);
        ConfigureDateTimeColumns(builder);
    }

    protected virtual void ConfigureDateTimeColumns(EntityTypeBuilder<MentionEntity> builder)
    {
        // Default: use datetime (SQL Server/MySQL compatible)
        builder.Property(e => e.PingTimeUtc).HasColumnType("datetime");
    }
}
