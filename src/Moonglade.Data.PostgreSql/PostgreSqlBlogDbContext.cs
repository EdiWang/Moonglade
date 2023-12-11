﻿using Microsoft.EntityFrameworkCore;
using Moonglade.Data.PostgreSql.Configurations;

namespace Moonglade.Data.PostgreSql;
public class PostgreSqlBlogDbContext : BlogDbContext
{
    public PostgreSqlBlogDbContext()
    {
    }

    public PostgreSqlBlogDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CommentConfiguration());
        modelBuilder.ApplyConfiguration(new CommentReplyConfiguration());
        modelBuilder.ApplyConfiguration(new PostConfiguration());
        modelBuilder.ApplyConfiguration(new PostCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new LocalAccountConfiguration());
        modelBuilder.ApplyConfiguration(new PingbackConfiguration());
        modelBuilder.ApplyConfiguration(new BlogThemeConfiguration());
        modelBuilder.ApplyConfiguration(new BlogAssetConfiguration());
        modelBuilder.ApplyConfiguration(new StyleSheetConfiguration());
        modelBuilder.ApplyConfiguration(new BlogConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new PageConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}