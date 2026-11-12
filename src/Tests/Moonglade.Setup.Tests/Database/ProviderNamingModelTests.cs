using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;

namespace Moonglade.Setup.Tests.Database;

public class ProviderNamingModelTests
{
    [Theory]
    [InlineData("PostgreSql", "blog_theme", "theme_name", "comment", "ip_address", "blog_configuration", "cfg_key")]
    [InlineData("SqlServer", "BlogTheme", "ThemeName", "Comment", "IPAddress", "BlogConfiguration", "CfgKey")]
    public void ProviderModel_UsesProviderSpecificTableAndColumnNames(
        string provider, string themeTable, string themeName, string commentTable, string ipAddress,
        string configurationTable, string configurationKey)
    {
        using BlogDbContext db = provider == "PostgreSql"
            ? new PostgreSqlBlogDbContext(new DbContextOptionsBuilder<PostgreSqlBlogDbContext>()
                .UseNpgsql("Host=localhost;Database=model_only").Options)
            : new SqlServerBlogDbContext(new DbContextOptionsBuilder<SqlServerBlogDbContext>()
                .UseSqlServer("Server=localhost;Database=model_only;Integrated Security=true").Options);

        var theme = db.Model.FindEntityType(typeof(BlogThemeEntity))!;
        Assert.Equal(themeTable, theme.GetTableName());
        Assert.Equal(themeName, theme.FindProperty(nameof(BlogThemeEntity.ThemeName))!.GetColumnName());
        var comment = db.Model.FindEntityType(typeof(CommentEntity))!;
        Assert.Equal(commentTable, comment.GetTableName());
        Assert.Equal(ipAddress, comment.FindProperty(nameof(CommentEntity.IPAddress))!.GetColumnName());
        var configuration = db.Model.FindEntityType(typeof(BlogConfigurationEntity))!;
        Assert.Equal(configurationTable, configuration.GetTableName());
        Assert.Equal(configurationKey, configuration.FindProperty(nameof(BlogConfigurationEntity.CfgKey))!.GetColumnName());
        Assert.Equal(provider == "PostgreSql" ? "base64data" : "Base64Data",
            db.Model.FindEntityType(typeof(BlogAssetEntity))!
                .FindProperty(nameof(BlogAssetEntity.Base64Data))!.GetColumnName());

        var account = db.Model.FindEntityType(typeof(OwnerAccountEntity))!;
        Assert.Equal(provider == "PostgreSql" ? "owner_account" : "OwnerAccount", account.GetTableName());
        Assert.Equal(provider == "PostgreSql" ? "normalized_email" : "NormalizedEmail",
            account.FindProperty(nameof(OwnerAccountEntity.NormalizedEmail))!.GetColumnName());
        var avatar = db.Model.FindEntityType(typeof(OwnerAvatarEntity))!;
        Assert.Equal(provider == "PostgreSql" ? "owner_avatar" : "OwnerAvatar", avatar.GetTableName());
        Assert.Equal(provider == "PostgreSql" ? "image_base64" : "ImageBase64",
            avatar.FindProperty(nameof(OwnerAvatarEntity.ImageBase64))!.GetColumnName());
        Assert.Equal(23, db.Model.GetEntityTypes().Count());
        if (provider == "PostgreSql")
        {
            foreach (var entity in db.Model.GetEntityTypes())
            {
                Assert.Matches("^[a-z][a-z0-9_]*$", entity.GetTableName()!);
                Assert.All(entity.GetProperties(), property =>
                    Assert.Matches("^[a-z][a-z0-9_]*$", property.GetColumnName()));
            }
        }

        var sql = db.BlogTheme.Where(theme => theme.ThemeName == "Custom").ToQueryString();
        Assert.Contains(themeTable, sql);
        Assert.Contains(themeName, sql);
        var ddl = db.Database.GenerateCreateScript();
        Assert.Contains(themeTable, ddl);
        Assert.Contains(themeName, ddl);
    }
}
