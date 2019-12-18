using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    public class FriendLinkConfiguration : IEntityTypeConfiguration<FriendLinkEntity>
    {
        public void Configure(EntityTypeBuilder<FriendLinkEntity> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).HasMaxLength(64);
            builder.Property(e => e.LinkUrl).HasMaxLength(256);
        }
    }
}
