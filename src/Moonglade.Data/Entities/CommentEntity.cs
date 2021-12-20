using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class CommentEntity
{
    public CommentEntity()
    {
        Replies = new HashSet<CommentReplyEntity>();
    }

    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string IPAddress { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public string CommentContent { get; set; }
    public Guid PostId { get; set; }
    public bool IsApproved { get; set; }

    public virtual PostEntity Post { get; set; }
    public virtual ICollection<CommentReplyEntity> Replies { get; set; }
}

[ExcludeFromCodeCoverage]
internal class CommentConfiguration : IEntityTypeConfiguration<CommentEntity>
{
    public void Configure(EntityTypeBuilder<CommentEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CommentContent).IsRequired();
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.Email).HasMaxLength(128);
        builder.Property(e => e.IPAddress).HasMaxLength(64);
        builder.Property(e => e.Username).HasMaxLength(64);
        builder.HasOne(d => d.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(d => d.PostId)
            .HasConstraintName("FK_Comment_Post");
    }
}