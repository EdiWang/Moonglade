using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.MySql.Configurations;

[ExcludeFromCodeCoverage]
internal class BlogAssetConfiguration : IEntityTypeConfiguration<BlogAssetEntity>
{
    public void Configure(EntityTypeBuilder<BlogAssetEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}