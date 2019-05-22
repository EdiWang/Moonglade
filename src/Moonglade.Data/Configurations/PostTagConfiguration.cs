using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    internal class PostTagConfiguration : IEntityTypeConfiguration<PostTagEntity>
    {
        public void Configure(EntityTypeBuilder<PostTagEntity> builder)
        {
            builder.HasKey(e => new { e.PostId, e.TagId });

            builder.HasOne(d => d.Post)
                   .WithMany(p => p.PostTag)
                   .HasForeignKey(d => d.PostId)
                   .HasConstraintName("FK_PostTag_Post");

            builder.HasOne(d => d.Tag)
                   .WithMany(p => p.PostTag)
                   .HasForeignKey(d => d.TagId)
                   .HasConstraintName("FK_PostTag_Tag");
        }
    }
}
