using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Configurations
{
    [ExcludeFromCodeCoverage]
    internal class CategoryConfiguration : IEntityTypeConfiguration<CategoryEntity>
    {
        public void Configure(EntityTypeBuilder<CategoryEntity> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.DisplayName).HasMaxLength(64);
            builder.Property(e => e.Note).HasMaxLength(128);
            builder.Property(e => e.RouteName).HasMaxLength(64);
        }
    }
}
