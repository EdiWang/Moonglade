using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Configurations.SqlServer
{
    [ExcludeFromCodeCoverage]
    public class BlogThemeConfiguration : IEntityTypeConfiguration<BlogThemeEntity>
    {
        public void Configure(EntityTypeBuilder<BlogThemeEntity> builder)
        {
            builder.Property(e => e.Id).UseIdentityColumn();
            builder.Property(e => e.ThemeName).HasMaxLength(32);
        }
    }
}
