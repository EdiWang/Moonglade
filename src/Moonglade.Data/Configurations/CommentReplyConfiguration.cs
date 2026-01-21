using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class CommentReplyConfiguration : IEntityTypeConfiguration<CommentReplyEntity>
{
    public void Configure(EntityTypeBuilder<CommentReplyEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.ReplyContent).IsRequired();
        ConfigureDateTimeColumns(builder);

        builder.HasOne(d => d.Comment)
            .WithMany(p => p.Replies)
            .HasForeignKey(d => d.CommentId)
            .HasConstraintName("FK_CommentReply_Comment");
    }

    protected virtual void ConfigureDateTimeColumns(EntityTypeBuilder<CommentReplyEntity> builder)
    {
        // Default: use datetime (SQL Server/MySQL compatible)
        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
    }
}
