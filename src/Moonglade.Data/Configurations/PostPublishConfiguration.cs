using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    internal class PostPublishConfiguration : IEntityTypeConfiguration<PostPublish>
    {
        public void Configure(EntityTypeBuilder<PostPublish> builder)
        {
            builder.HasKey(e => e.PostId);
            builder.Property(e => e.PostId).ValueGeneratedNever();
            builder.Property(e => e.LastModifiedUtc).HasColumnType("datetime");

            builder.Property(e => e.PubDateUtc)
                   .HasColumnType("datetime");

            builder.Property(e => e.PublisherIp).HasMaxLength(64);
            builder.Property(e => e.ContentLanguageCode).HasMaxLength(8);

            builder.HasOne(d => d.Post)
                   .WithOne(p => p.PostPublish)
                   .HasForeignKey<PostPublish>(d => d.PostId)
                   .HasConstraintName("FK_PostPublish_Post");
        }
    }
}
