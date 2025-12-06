using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

public class WidgetConfiguration : IEntityTypeConfiguration<WidgetEntity>
{
    public void Configure(EntityTypeBuilder<WidgetEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(100);
        builder.Property(e => e.WidgetType)
            .HasMaxLength(50)
            .HasConversion<string>();
        builder.Property(e => e.ContentType)
            .HasMaxLength(25)
            .HasConversion<string>();
        builder.Property(e => e.ContentCode).HasMaxLength(2000);
    }
}
