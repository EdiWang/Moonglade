using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    internal class PostCategoryConfiguration: IEntityTypeConfiguration<PostCategory>
    {
        public void Configure(EntityTypeBuilder<PostCategory> builder)
        {
            builder.HasKey(e => new { e.PostId, e.CategoryId });

            builder.HasOne(d => d.Category)
                   .WithMany(p => p.PostCategory)
                   .HasForeignKey(d => d.CategoryId)
                   .HasConstraintName("FK_PostCategory_Category");

            builder.HasOne(d => d.Post)
                   .WithMany(p => p.PostCategory)
                   .HasForeignKey(d => d.PostId)
                   .HasConstraintName("FK_PostCategory_Post");
        }
    }
}
