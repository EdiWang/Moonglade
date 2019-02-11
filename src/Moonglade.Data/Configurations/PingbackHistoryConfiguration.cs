using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    internal class PingbackHistoryConfiguration : IEntityTypeConfiguration<PingbackHistory>
    {
        public void Configure(EntityTypeBuilder<PingbackHistory> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Direction).HasMaxLength(16);
            builder.Property(e => e.Domain).HasMaxLength(256);
            builder.Property(e => e.PingTimeUtc).HasColumnType("datetime");
            builder.Property(e => e.SourceIp).HasMaxLength(64);
            builder.Property(e => e.SourceTitle).HasMaxLength(256);
            builder.Property(e => e.SourceUrl).HasMaxLength(256);
        }
    }
}
