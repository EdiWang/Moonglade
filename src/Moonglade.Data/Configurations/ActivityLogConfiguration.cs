using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLogEntity>
{
    public void Configure(EntityTypeBuilder<ActivityLogEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.ActorId).HasMaxLength(100);
        builder.Property(e => e.Operation).HasMaxLength(100);
        builder.Property(e => e.TargetName).HasMaxLength(200);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasMaxLength(512);
        ConfigureDateTimeColumns(builder);
    }

    protected virtual void ConfigureDateTimeColumns(EntityTypeBuilder<ActivityLogEntity> builder)
    {
        // Default: use datetime (SQL Server/MySQL compatible)
        builder.Property(e => e.EventTimeUtc).HasColumnType("datetime");
    }
}
