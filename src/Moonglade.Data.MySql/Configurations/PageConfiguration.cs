using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.MySql.Configurations;

internal class PageConfiguration : Data.Configurations.PageConfiguration
{
    protected override void ConfigureDateTimeColumns(EntityTypeBuilder<PageEntity> builder)
    {
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.UpdateTimeUtc).HasColumnType("datetime");
    }
}