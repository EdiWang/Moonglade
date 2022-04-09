using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Moonglade.Data.Entities;

public class BlogConfigurationEntity
{
    public int Id { get; set; }

    public string CfgKey { get; set; }

    public string CfgValue { get; set; }

    public DateTime? LastModifiedTimeUtc { get; set; }
}


internal class BlogConfigurationConfiguration : IEntityTypeConfiguration<BlogConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<BlogConfigurationEntity> builder)
    {
        builder.Property(e => e.CfgKey).HasMaxLength(64);
    }
}