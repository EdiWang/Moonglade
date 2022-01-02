using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.MySql.Configurations
{
    [ExcludeFromCodeCoverage]
    internal class PostExtensionConfiguration : IEntityTypeConfiguration<PostExtensionEntity>
    {
        public void Configure(EntityTypeBuilder<PostExtensionEntity> builder)
        {
            builder.HasKey(e => e.PostId);
            builder.Property(e => e.PostId).ValueGeneratedNever();

            builder.HasOne(d => d.Post)
                .WithOne(p => p.PostExtension)
                .HasForeignKey<PostExtensionEntity>(d => d.PostId)
                .HasConstraintName("FK_PostExtension_Post");
        }
    }
}
