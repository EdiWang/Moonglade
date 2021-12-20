using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class CommentReplyEntity
{
    public Guid Id { get; set; }
    public string ReplyContent { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public Guid? CommentId { get; set; }

    public virtual CommentEntity Comment { get; set; }
}

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