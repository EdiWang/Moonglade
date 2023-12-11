﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Generated.Entities;

namespace Moonglade.Data.SqlServer.Configurations;


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