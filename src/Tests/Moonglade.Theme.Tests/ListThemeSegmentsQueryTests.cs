using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme.Tests;

public class ListThemeSegmentsQueryTests
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
    public async Task HandleAsync_NoCustomThemes_ReturnsOnlySystemThemes()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Count); // 10 system themes
        Assert.All(result, theme => Assert.Equal(ThemeType.System, theme.ThemeType));
    }

    [Fact]
    public async Task HandleAsync_WithCustomThemes_ReturnsBothSystemAndCustomThemes()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.AddRange(
            new BlogThemeEntity
            {
                Id = 1,
                ThemeName = "Custom Theme 1",
                CssRules = """{"--color": "blue"}""",
                ThemeType = ThemeType.User
            },
            new BlogThemeEntity
            {
                Id = 2,
                ThemeName = "Custom Theme 2",
                CssRules = """{"--color": "red"}""",
                ThemeType = ThemeType.User
            }
        );
        await db.SaveChangesAsync();

        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12, result.Count); // 10 system + 2 custom
        Assert.Equal(10, result.Count(t => t.ThemeType == ThemeType.System));
        Assert.Equal(2, result.Count(t => t.ThemeType == ThemeType.User));
    }

    [Fact]
    public async Task HandleAsync_SystemThemesAppearFirst()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 1,
            ThemeName = "Custom Theme",
            CssRules = """{"--color": "blue"}""",
            ThemeType = ThemeType.User
        });
        await db.SaveChangesAsync();

        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // First 10 should be system themes
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(ThemeType.System, result[i].ThemeType);
        }

        // 11th should be custom theme
        Assert.Equal(ThemeType.User, result[10].ThemeType);
        Assert.Equal("Custom Theme", result[10].ThemeName);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAllSystemThemeIds()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        var expectedIds = new[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109 };
        var actualIds = result.Select(t => t.Id).ToArray();

        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public async Task HandleAsync_SystemThemesHaveValidProperties()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.All(result, theme =>
        {
            Assert.NotNull(theme.ThemeName);
            Assert.NotEmpty(theme.ThemeName);
            Assert.NotNull(theme.CssRules);
            Assert.NotEmpty(theme.CssRules);
            Assert.Contains("--accent-color1", theme.CssRules);
            Assert.Contains("--accent-color2", theme.CssRules);
        });
    }

    [Fact]
    public async Task HandleAsync_CustomThemesPreserveAllProperties()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 50,
            ThemeName = "My Custom Theme",
            CssRules = """{"--primary": "#123456", "--secondary": "#abcdef"}""",
            AdditionalProps = """{"font": "Arial"}""",
            ThemeType = ThemeType.User
        });
        await db.SaveChangesAsync();

        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        var customTheme = result.FirstOrDefault(t => t.Id == 50);
        Assert.NotNull(customTheme);
        Assert.Equal("My Custom Theme", customTheme.ThemeName);
        Assert.Contains("--primary", customTheme.CssRules);
        Assert.Contains("--secondary", customTheme.CssRules);
        Assert.Equal("""{"font": "Arial"}""", customTheme.AdditionalProps);
        Assert.Equal(ThemeType.User, customTheme.ThemeType);
    }

    #endregion

    #region HandleAsync Tests - Edge Cases

    [Fact]
    public async Task HandleAsync_MultipleCustomThemes_AllIncludedAfterSystemThemes()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.AddRange(
            new BlogThemeEntity { Id = 1, ThemeName = "Theme 1", CssRules = "{}", ThemeType = ThemeType.User },
            new BlogThemeEntity { Id = 2, ThemeName = "Theme 2", CssRules = "{}", ThemeType = ThemeType.User },
            new BlogThemeEntity { Id = 3, ThemeName = "Theme 3", CssRules = "{}", ThemeType = ThemeType.User },
            new BlogThemeEntity { Id = 4, ThemeName = "Theme 4", CssRules = "{}", ThemeType = ThemeType.User },
            new BlogThemeEntity { Id = 5, ThemeName = "Theme 5", CssRules = "{}", ThemeType = ThemeType.User }
        );
        await db.SaveChangesAsync();

        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(15, result.Count); // 10 system + 5 custom

        // Verify all custom themes are present
        for (int i = 1; i <= 5; i++)
        {
            Assert.Contains(result, t => t.Id == i && t.ThemeName == $"Theme {i}");
        }
    }

    [Fact]
    public async Task HandleAsync_CancellationToken_DoesNotThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var cts = new CancellationTokenSource();
        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Count); // 10 system themes
    }

    [Fact]
    public async Task HandleAsync_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 1, ThemeName = "Custom", CssRules = "{}", ThemeType = ThemeType.User
        });
        await db.SaveChangesAsync();

        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result1 = await handler.HandleAsync(query, CancellationToken.None);
        var result2 = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(result1.Count, result2.Count);
        Assert.Equal(result1.Select(t => t.Id), result2.Select(t => t.Id));
    }

    #endregion

    #region HandleAsync Tests - Verification

    [Fact]
    public async Task HandleAsync_AlwaysQueriesDatabase()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Count); // 10 system themes from factory, 0 from DB
    }

    [Fact]
    public async Task HandleAsync_ReturnsNewListInstance()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 1, ThemeName = "Custom", CssRules = "{}", ThemeType = ThemeType.User
        });
        await db.SaveChangesAsync();

        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result1 = await handler.HandleAsync(query, CancellationToken.None);
        var result2 = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        // The result should be a new list each time
        Assert.NotSame(result1, result2);
    }

    [Theory]
    [InlineData(0)]  // No custom themes
    [InlineData(1)]  // One custom theme
    [InlineData(5)]  // Multiple custom themes
    [InlineData(10)] // Equal to system themes count
    [InlineData(15)] // More than system themes
    public async Task HandleAsync_VariousCustomThemeCounts_ReturnsCorrectTotal(int customCount)
    {
        // Arrange
        using var db = CreateDbContext();
        for (int i = 1; i <= customCount; i++)
        {
            db.BlogTheme.Add(new BlogThemeEntity
            {
                Id = i,
                ThemeName = $"Theme {i}",
                CssRules = "{}",
                ThemeType = ThemeType.User
            });
        }
        await db.SaveChangesAsync();

        var handler = new ListThemeSegmentsQueryHandler(db);
        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(10 + customCount, result.Count);
    }

    #endregion
}
