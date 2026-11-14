using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLogEntity>
{
    public void Configure(EntityTypeBuilder<ActivityLogEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.EventKey).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EventTimeUtc).IsRequired();
        builder.Property(e => e.ActorKind).IsRequired();
        builder.Property(e => e.ActorDisplayName).HasMaxLength(100);
        builder.Property(e => e.TargetId).HasMaxLength(100);
        builder.Property(e => e.TargetDisplayName).HasMaxLength(200);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasMaxLength(512);
    }
}
