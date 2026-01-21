using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.PostgreSql.Configurations;

public class BlogThemeConfiguration : Data.Configurations.BlogThemeConfiguration
{
    protected override void ConfigureIdentityColumn(EntityTypeBuilder<BlogThemeEntity> builder)
    {
        builder.Property(e => e.Id).UseIdentityColumn();
    }
}