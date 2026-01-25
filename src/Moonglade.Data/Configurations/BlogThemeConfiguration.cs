using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class BlogThemeConfiguration : IEntityTypeConfiguration<BlogThemeEntity>
{
    public void Configure(EntityTypeBuilder<BlogThemeEntity> builder)
    {
        ConfigureIdentityColumn(builder);
        builder.Property(e => e.ThemeName).HasMaxLength(32);
    }

    protected virtual void ConfigureIdentityColumn(EntityTypeBuilder<BlogThemeEntity> builder)
    {
        // Default: ValueGeneratedOnAdd (MySQL compatible)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
    }
}
