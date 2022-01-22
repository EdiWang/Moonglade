using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class BlogConfigurationEntity
{
    public int Id { get; set; }

    public string CfgKey { get; set; }

    public string CfgValue { get; set; }

    public DateTime? LastModifiedTimeUtc { get; set; }
}

[ExcludeFromCodeCoverage]
internal class BlogConfigurationConfiguration : IEntityTypeConfiguration<BlogConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<BlogConfigurationEntity> builder)
    {
        builder.Property(e => e.CfgKey).HasMaxLength(64);
    }
}