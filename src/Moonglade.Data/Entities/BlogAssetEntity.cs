using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Moonglade.Data.Entities;

public class BlogAssetEntity
{
    public Guid Id { get; set; }

    public string Base64Data { get; set; }

    public DateTime LastModifiedTimeUtc { get; set; }
}

[ExcludeFromCodeCoverage]
internal class BlogAssetConfiguration : IEntityTypeConfiguration<BlogAssetEntity>
{
    public void Configure(EntityTypeBuilder<BlogAssetEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}