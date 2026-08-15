using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class PostViewDailyConfiguration : IEntityTypeConfiguration<PostViewDailyEntity>
{
    public void Configure(EntityTypeBuilder<PostViewDailyEntity> builder)
    {
        builder.HasKey(nameof(PostViewDailyEntity.PostId), nameof(PostViewDailyEntity.ViewDateUtc));
        builder.HasIndex(e => e.ViewDateUtc);
        builder.Property(e => e.PostId).ValueGeneratedNever();
        builder.Property(e => e.ViewDateUtc).HasColumnType("date");
    }
}
