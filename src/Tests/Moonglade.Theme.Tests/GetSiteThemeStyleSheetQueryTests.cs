using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme.Tests;

public class GetSiteThemeStyleSheetQueryTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    #region HandleAsync Tests - Success Cases

    [Fact]
    public async Task HandleAsync_SystemTheme_ReturnsValidCss()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(100);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith(":root {", result);
        Assert.Contains("--accent-color1", result);
        Assert.Contains("--accent-color2", result);
    }

    [Fact]
    public async Task HandleAsync_CustomTheme_ReturnsValidCss()
    {
        // Arrange
        using var db = CreateDbContext();
        var cssRules = """{"--primary-color": "#ff0000", "--secondary-color": "#00ff00"}""";
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 1,
            ThemeName = "Custom Theme",
            CssRules = cssRules,
            ThemeType = ThemeType.User
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(1);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(":root {--primary-color: #ff0000;--secondary-color: #00ff00;}", result);
    }

    [Fact]
    public async Task HandleAsync_ValidCssRules_GeneratesCorrectCss()
    {
        // Arrange
        using var db = CreateDbContext();
        var cssRules = """{"--color-1": "blue", "--color-2": "red", "--color-3": "green"}""";
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = cssRules
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Contains("--color-1: blue;", result);
        Assert.Contains("--color-2: red;", result);
        Assert.Contains("--color-3: green;", result);
    }

    [Fact]
    public async Task HandleAsync_CssRulesWithWhitespace_FiltersOutEmptyRules()
    {
        // Arrange
        using var db = CreateDbContext();
        var cssRules = """{"--valid-color": "blue", "": "red", "--another-color": "", "  ": "green"}""";
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = cssRules
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Contains("--valid-color: blue;", result);
        Assert.DoesNotContain(": red;", result);
        Assert.DoesNotContain(": green;", result);
    }

    [Fact]
    public async Task HandleAsync_AllSystemThemeIds_ReturnValidCss()
    {
        // Arrange - System themes are 100-109 (10 themes)
        using var db = CreateDbContext();
        var handler = new GetStyleSheetQueryHandler(db);
        var systemThemes = ThemeFactory.GetSystemThemes().ToList();

        // Act & Assert
        foreach (var theme in systemThemes)
        {
            var query = new GetSiteThemeStyleSheetQuery(theme.Id);
            var result = await handler.HandleAsync(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.StartsWith(":root {", result);
        }
    }

    #endregion

    #region HandleAsync Tests - Null/Not Found Cases

    [Fact]
    public async Task HandleAsync_CustomThemeNotFound_FallsBackToDefaultSystemTheme()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(99);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith(":root {", result);
    }

    [Fact]
    public async Task HandleAsync_ThemeIsNull_FallsBackToDefaultSystemTheme()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(999);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        // When custom theme not found, it falls back to default system theme (100)
        Assert.NotNull(result);
    }

    #endregion

    #region HandleAsync Tests - Error Cases

    [Fact]
    public async Task HandleAsync_EmptyCssRules_ThrowsInvalidDataException()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = ""
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("Theme id '50' has empty CSS rules", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WhitespaceCssRules_ThrowsInvalidDataException()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = "   "
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("has empty CSS rules", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidJson_ThrowsInvalidDataException()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = "invalid json {{{["
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("Theme id '50' has invalid JSON in CssRules", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public async Task HandleAsync_NullDeserializedRules_ThrowsInvalidDataException()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = "null"
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("Theme id '50' CssRules deserialized to empty or null", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_EmptyJsonObject_ThrowsInvalidDataException()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = "{}"
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("Theme id '50' CssRules deserialized to empty or null", exception.Message);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(99)]  // Just below system theme range
    [InlineData(111)] // Just above system theme range
    [InlineData(1)]   // Small custom theme ID
    [InlineData(50)]  // Mid-range custom theme ID
    public async Task HandleAsync_CustomThemeId_QueriesDatabase(int themeId)
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = themeId,
            ThemeName = "Test",
            CssRules = """{"--color": "blue"}"""
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(themeId);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("--color: blue;", result);
    }

    [Theory]
    [InlineData(100)] // System theme start
    [InlineData(105)] // System theme middle
    [InlineData(109)] // System theme end
    public async Task HandleAsync_SystemThemeId_DoesNotNeedDatabase(int themeId)
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(themeId);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith(":root {", result);
    }

    [Fact]
    public async Task HandleAsync_CancellationToken_PassedToDatabase()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = """{"--color": "blue"}"""
        });
        await db.SaveChangesAsync();

        var cts = new CancellationTokenSource();
        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act
        var result = await handler.HandleAsync(query, cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandleAsync_CssValueWithScriptInjection_StripsAngleBrackets()
    {
        // Arrange
        using var db = CreateDbContext();
        var cssRules = """{"--color": "</style><script>alert(1)</script><style>"}""";
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = cssRules
        });
        await db.SaveChangesAsync();

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain("</style>", result);
        Assert.DoesNotContain("<script>", result);
    }

    #endregion
}
