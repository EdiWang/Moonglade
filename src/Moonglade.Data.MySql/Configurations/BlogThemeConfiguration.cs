using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.MySql.Configurations;

public class BlogThemeConfiguration : Data.Configurations.BlogThemeConfiguration
{
    protected override void ConfigureIdentityColumn(EntityTypeBuilder<BlogThemeEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
    }
}