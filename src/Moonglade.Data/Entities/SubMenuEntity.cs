using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class SubMenuEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public string Url { get; set; }

    public bool IsOpenInNewTab { get; set; }

    public Guid MenuId { get; set; }

    public virtual MenuEntity Menu { get; set; }
}

[ExcludeFromCodeCoverage]
internal class SubMenuConfiguration : IEntityTypeConfiguration<SubMenuEntity>
{
    public void Configure(EntityTypeBuilder<SubMenuEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(64);
        builder.Property(e => e.Url).HasMaxLength(256);
        builder.HasOne(d => d.Menu)
            .WithMany(p => p.SubMenus)
            .HasForeignKey(d => d.MenuId);
    }
}