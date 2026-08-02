using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessageEntity> builder)
    {
        builder.ToTable("EmailOutboxMessage");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MessageType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.DistributionList).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.MessageBody).IsRequired();
        builder.Property(e => e.LockedBy).HasMaxLength(128);
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(
            nameof(EmailOutboxMessageEntity.Status),
            nameof(EmailOutboxMessageEntity.NotBeforeUtc),
            nameof(EmailOutboxMessageEntity.LockedUntilUtc),
            nameof(EmailOutboxMessageEntity.CreatedTimeUtc))
            .HasDatabaseName("IX_EmailOutboxMessage_Dequeue");
    }
}
