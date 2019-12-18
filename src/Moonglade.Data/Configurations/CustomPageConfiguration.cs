using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    public class CustomPageConfiguration : IEntityTypeConfiguration<CustomPageEntity>
    {
        public void Configure(EntityTypeBuilder<CustomPageEntity> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.Title).HasMaxLength(128);
            builder.Property(e => e.RouteName).HasMaxLength(128);
        }
    }
}
