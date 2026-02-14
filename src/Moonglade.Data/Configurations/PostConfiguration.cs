using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<PostEntity>
{
    public void Configure(EntityTypeBuilder<PostEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CommentEnabled);
        builder.Property(e => e.ContentAbstract).HasMaxLength(1024);
        builder.Property(e => e.ContentLanguageCode).HasMaxLength(8);

        ConfigureDateTimeColumns(builder);
        builder.Property(e => e.PostContent);

        builder.Property(e => e.Author).HasMaxLength(64);
        builder.Property(e => e.Slug).HasMaxLength(128);
        builder.Property(e => e.Title).HasMaxLength(128);
        builder.Property(e => e.RouteLink).HasMaxLength(256);
        builder.Property(e => e.Keywords).HasMaxLength(256);

        // Convert enum to string for database storage
        builder.Property(e => e.PostStatus)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<PostStatus>(v, true))
            .HasMaxLength(32);
    }

    protected virtual void ConfigureDateTimeColumns(EntityTypeBuilder<PostEntity> builder)
    {
        // Default: use datetime (SQL Server/MySQL compatible)
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.PubDateUtc).HasColumnType("datetime");
        builder.Property(e => e.LastModifiedUtc).HasColumnType("datetime");
    }
}
