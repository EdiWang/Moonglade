﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Generated.Entities;

namespace Moonglade.Data.PostgreSql.Configurations;

internal class StyleSheetConfiguration : IEntityTypeConfiguration<StyleSheetEntity>
{
    public void Configure(EntityTypeBuilder<StyleSheetEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.FriendlyName).HasMaxLength(32);
        builder.Property(e => e.Hash).HasMaxLength(64);
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("timestamp");
    }
}