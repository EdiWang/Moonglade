using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class MenuEntity
{
    public MenuEntity()
    {
        SubMenus = new HashSet<SubMenuEntity>();
    }

    public Guid Id { get; set; }

    public string Title { get; set; }

    public string Url { get; set; }

    public string Icon { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsOpenInNewTab { get; set; }

    public virtual ICollection<SubMenuEntity> SubMenus { get; set; }
}

[ExcludeFromCodeCoverage]
internal class MenuConfiguration : IEntityTypeConfiguration<MenuEntity>
{
    public void Configure(EntityTypeBuilder<MenuEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(64);
        builder.Property(e => e.Url).HasMaxLength(256);
        builder.Property(e => e.Icon).HasMaxLength(64);
    }
}