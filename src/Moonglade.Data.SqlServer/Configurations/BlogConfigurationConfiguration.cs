using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

internal class BlogConfigurationConfiguration: IEntityTypeConfiguration<BlogConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<BlogConfigurationEntity> builder)
    {
        builder.Property(e => e.LastModifiedTimeUtc).HasColumnType("datetime");
    }
}