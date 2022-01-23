using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

internal class BlogAssetConfiguration : IEntityTypeConfiguration<BlogAssetEntity>
{
    public void Configure(EntityTypeBuilder<BlogAssetEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}