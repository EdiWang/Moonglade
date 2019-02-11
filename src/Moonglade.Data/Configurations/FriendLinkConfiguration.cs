using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    public class FriendLinkConfiguration : IEntityTypeConfiguration<FriendLink>
    {
        public void Configure(EntityTypeBuilder<FriendLink> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).HasMaxLength(64);
            builder.Property(e => e.LinkUrl).HasMaxLength(256);
        }
    }
}
