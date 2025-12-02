using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.MySql.Configurations;

public class WidgetConfiguration : IEntityTypeConfiguration<WidgetEntity>
{
    public void Configure(EntityTypeBuilder<WidgetEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(100);
        builder.Property(e => e.WidgetType).HasMaxLength(50);
    }
}

public class WidgetLinkItemConfiguration : IEntityTypeConfiguration<WidgetLinkItemEntity>
{
    public void Configure(EntityTypeBuilder<WidgetLinkItemEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(100);
        builder.Property(e => e.Url).HasMaxLength(500);
        builder.Property(e => e.IconName).HasMaxLength(50);
    }
}