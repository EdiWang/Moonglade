using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    [ExcludeFromCodeCoverage]
    internal class TagConfiguration : IEntityTypeConfiguration<TagEntity>
    {
        public void Configure(EntityTypeBuilder<TagEntity> builder)
        {
            builder.Property(e => e.DisplayName).HasMaxLength(32);
            builder.Property(e => e.NormalizedName).HasMaxLength(32);
        }
    }
}
