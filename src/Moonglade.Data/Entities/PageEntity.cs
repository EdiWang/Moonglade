using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class PageEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string MetaDescription { get; set; }
    public string HtmlContent { get; set; }
    public string CssContent { get; set; }
    public bool HideSidebar { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public DateTime? UpdateTimeUtc { get; set; }
}

[ExcludeFromCodeCoverage]
internal class PageConfiguration : IEntityTypeConfiguration<PageEntity>
{
    public void Configure(EntityTypeBuilder<PageEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(128);
        builder.Property(e => e.Slug).HasMaxLength(128);
        builder.Property(e => e.MetaDescription).HasMaxLength(256);
    }
}