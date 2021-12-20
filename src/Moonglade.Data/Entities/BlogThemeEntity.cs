using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class BlogThemeEntity
{
    public int Id { get; set; }
    public string ThemeName { get; set; }
    public string CssRules { get; set; }
    public string AdditionalProps { get; set; }
    public ThemeType ThemeType { get; set; }
}

public enum ThemeType
{
    System = 0,
    User = 1
}

[ExcludeFromCodeCoverage]
public class BlogThemeConfiguration : IEntityTypeConfiguration<BlogThemeEntity>
{
    public void Configure(EntityTypeBuilder<BlogThemeEntity> builder)
    {
        builder.Property(e => e.Id).UseIdentityColumn();
        builder.Property(e => e.ThemeName).HasMaxLength(32);
    }
}