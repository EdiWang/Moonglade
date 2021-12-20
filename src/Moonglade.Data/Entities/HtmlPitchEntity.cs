using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class HtmlPitchEntity
{
    public int Id { get; set; }

    public string PitchKey { get; set; }

    public string HtmlCode { get; set; }
}

[ExcludeFromCodeCoverage]
public class HtmlPitchConfiguration : IEntityTypeConfiguration<HtmlPitchEntity>
{
    public void Configure(EntityTypeBuilder<HtmlPitchEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.PitchKey).HasMaxLength(32);
    }
}