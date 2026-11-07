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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(1);

        // Act
        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(":root {--primary-color: #ff0000;--secondary-color: #00ff00;}", result);
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act
        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("--valid-color: blue;", result);
        Assert.DoesNotContain(": red;", result);
        Assert.DoesNotContain(": green;", result);
    }

    [Fact]
    public async Task HandleAsync_CustomThemeNotFound_FallsBackToDefaultSystemTheme()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(99);

        // Act
        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(":root {--accent-color1: #2A579A;--accent-color2: #5579AE;}", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_BlankCssRules_ThrowsInvalidDataException(string cssRules)
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = cssRules
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => handler.HandleAsync(query, TestContext.Current.CancellationToken));

        Assert.Contains("Theme id '50' has empty CSS rules", exception.Message);
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => handler.HandleAsync(query, TestContext.Current.CancellationToken));

        Assert.Contains("Theme id '50' has invalid JSON in CssRules", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task HandleAsync_EmptyDeserializedRules_ThrowsInvalidDataException(string cssRules)
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "Test",
            CssRules = cssRules
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => handler.HandleAsync(query, TestContext.Current.CancellationToken));

        Assert.Contains("Theme id '50' CssRules deserialized to empty or null", exception.Message);
    }

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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(themeId);

        // Act
        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("--color: blue;", result);
    }

    [Theory]
    [InlineData(100)] // System theme start
    [InlineData(101)]
    [InlineData(102)]
    [InlineData(103)]
    [InlineData(104)]
    [InlineData(105)]
    [InlineData(106)]
    [InlineData(107)]
    [InlineData(108)]
    [InlineData(109)] // System theme end
    public async Task HandleAsync_SystemThemeId_DoesNotNeedDatabase(int themeId)
    {
        // Arrange
        using var db = CreateDbContext();
        await db.DisposeAsync();
        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(themeId);

        // Act
        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith(":root {", result);
        Assert.Contains("--accent-color1: #", result);
        Assert.Contains("--accent-color2: #", result);
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStyleSheetQueryHandler(db);
        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act
        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain("</style>", result);
        Assert.DoesNotContain("<script>", result);
    }

}
