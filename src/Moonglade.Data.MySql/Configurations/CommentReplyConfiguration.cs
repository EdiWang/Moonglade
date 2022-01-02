using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.MySql.Configurations
{
    [ExcludeFromCodeCoverage]
    internal class CommentReplyConfiguration : IEntityTypeConfiguration<CommentReplyEntity>
    {
        public void Configure(EntityTypeBuilder<CommentReplyEntity> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
            builder.HasOne(d => d.Comment)
                .WithMany(p => p.Replies)
                .HasForeignKey(d => d.CommentId);
        }
    }
}
