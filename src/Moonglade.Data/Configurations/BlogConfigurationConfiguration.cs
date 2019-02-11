using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    public class BlogConfigurationConfiguration : IEntityTypeConfiguration<BlogConfiguration>
    {
        public void Configure(EntityTypeBuilder<BlogConfiguration> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.CfgKey).HasMaxLength(64);
            builder.Property(e => e.CfgValue);
            builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
        }
    }
}
