using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    internal class PostExtensionConfiguration: IEntityTypeConfiguration<PostExtension>
    {
        public void Configure(EntityTypeBuilder<PostExtension> builder)
        {
            builder.HasKey(e => e.PostId);
            builder.Property(e => e.PostId).ValueGeneratedNever();

            builder.HasOne(d => d.Post)
                   .WithOne(p => p.PostExtension)
                   .HasForeignKey<PostExtension>(d => d.PostId)
                   .HasConstraintName("FK_PostExtension_Post");
        }
    }
}
