﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.MySql.Configurations;


internal class PostConfiguration : IEntityTypeConfiguration<PostEntity>
{
    public void Configure(EntityTypeBuilder<PostEntity> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CommentEnabled);
        builder.Property(e => e.ContentAbstract).HasMaxLength(1024);
        builder.Property(e => e.ContentLanguageCode).HasMaxLength(8);

        builder.Property(e => e.CreateTimeUtc).HasColumnType("datetime");
        builder.Property(e => e.PubDateUtc).HasColumnType("datetime");
        builder.Property(e => e.LastModifiedUtc).HasColumnType("datetime");
        builder.Property(e => e.PostContent);

        builder.Property(e => e.Author).HasMaxLength(64);
        builder.Property(e => e.Slug).HasMaxLength(128);
        builder.Property(e => e.Title).HasMaxLength(128);
        builder.Property(e => e.HeroImageUrl).HasMaxLength(256);
        builder.Property(e => e.RouteLink).HasMaxLength(256);
        builder.Property(e => e.Keywords).HasMaxLength(256);
    }
}