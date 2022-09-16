using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

public class EmailNotificationConfiguration : IEntityTypeConfiguration<EmailNotificationEntity>
{
    public void Configure(EntityTypeBuilder<EmailNotificationEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.DistributionList).IsRequired().HasMaxLength(1024);
        builder.Property(e => e.MessageType).IsRequired().HasMaxLength(32);
        builder.Property(e => e.MessageBody).HasMaxLength(2048);
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.SentTimeUtc).HasColumnType("datetime");
    }
}