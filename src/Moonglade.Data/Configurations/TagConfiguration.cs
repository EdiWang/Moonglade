using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    internal class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.Property(e => e.DisplayName).HasMaxLength(32);
            builder.Property(e => e.NormalizedName).HasMaxLength(32);
        }
    }
}
