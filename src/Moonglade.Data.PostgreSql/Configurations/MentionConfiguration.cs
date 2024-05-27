using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.PostgreSql.Configurations;


internal class MentionConfiguration : IEntityTypeConfiguration<MentionEntity>
{
    public void Configure(EntityTypeBuilder<MentionEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.PingTimeUtc).HasColumnType("timestamp");
    }
}