using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class BlogAssetConfiguration : IEntityTypeConfiguration<BlogAssetEntity>
{
    public void Configure(EntityTypeBuilder<BlogAssetEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Base64Data).IsRequired();
        ConfigureDateTimeColumns(builder);
    }

    protected virtual void ConfigureDateTimeColumns(EntityTypeBuilder<BlogAssetEntity> builder)
    {
        // Default: use datetime (SQL Server/MySQL compatible)
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}
