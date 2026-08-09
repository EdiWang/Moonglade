using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class SiteVerificationFileConfiguration : IEntityTypeConfiguration<SiteVerificationFileEntity>
{
    public void Configure(EntityTypeBuilder<SiteVerificationFileEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(128);
        builder.Property(e => e.NormalizedFileName).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Content).IsRequired().HasMaxLength(65536);
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(64);
        builder.HasIndex(e => e.NormalizedFileName).IsUnique();
        ConfigureDateTimeColumns(builder);
    }

    protected virtual void ConfigureDateTimeColumns(EntityTypeBuilder<SiteVerificationFileEntity> builder)
    {
        builder.Property(e => e.CreatedTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}
