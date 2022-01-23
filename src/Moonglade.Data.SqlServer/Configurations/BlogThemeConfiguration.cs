using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.SqlServer.Configurations;

[ExcludeFromCodeCoverage]
public class BlogThemeConfiguration : IEntityTypeConfiguration<BlogThemeEntity>
{
    public void Configure(EntityTypeBuilder<BlogThemeEntity> builder)
    {
        builder.Property(e => e.Id).UseIdentityColumn();
        builder.Property(e => e.ThemeName).HasMaxLength(32);
    }
}