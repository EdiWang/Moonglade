using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    public class MenuConfiguration : IEntityTypeConfiguration<MenuEntity>
    {
        public void Configure(EntityTypeBuilder<MenuEntity> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).HasMaxLength(64);
            builder.Property(e => e.Url).HasMaxLength(256);
            builder.Property(e => e.Icon).HasMaxLength(64);
        }
    }
}
