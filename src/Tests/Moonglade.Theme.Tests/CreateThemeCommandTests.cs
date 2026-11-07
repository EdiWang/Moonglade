using Moonglade.Data;
using Moonglade.Data.Entities;
using System.Text.Json;

namespace Moonglade.Theme.Tests;

public class CreateThemeCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ThemeAlreadyExists_ReturnsMinusOne()
    {
        // Arrange
        var themeName = "Existing Theme";
        var rules = new Dictionary<string, string>
        {
            { "--accent-color1", "#2A579A" },
            { "--accent-color2", "#FFFFFF" }
        };
        var command = new CreateThemeCommand(themeName, rules);

        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            ThemeName = themeName,
            CssRules = "{}",
            ThemeType = ThemeType.User
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateThemeCommandHandler(db);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task HandleAsync_NewTheme_CreatesThemeAndReturnsId()
    {
        // Arrange
        var themeName = "New Theme";
        var rules = new Dictionary<string, string>
        {
            { "--accent-color1", "#2A579A" },
            { "--accent-color2", "#FFFFFF" },
            { "--font-family", "Arial, sans-serif" },
            { "--border-radius", "5px" }
        };
        var command = new CreateThemeCommand(themeName, rules);

        using var db = CreateDbContext();
        var handler = new CreateThemeCommandHandler(db);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result > 0);

        var savedEntity = await db.BlogTheme.FindAsync([result], TestContext.Current.CancellationToken);
        Assert.NotNull(savedEntity);
        Assert.Equal(themeName, savedEntity.ThemeName);
        Assert.Equal(ThemeType.User, savedEntity.ThemeType);

        var deserializedRules = JsonSerializer.Deserialize<Dictionary<string, string>>(savedEntity.CssRules);
        Assert.NotNull(deserializedRules);
        Assert.Equal(rules.Count, deserializedRules.Count);
        Assert.All(rules, rule => Assert.Equal(rule.Value, deserializedRules[rule.Key]));
    }

    [Fact]
    public async Task HandleAsync_ThemeNameWithWhitespace_TrimsNameBeforeCreating()
    {
        // Arrange
        var themeName = "  Theme With Spaces  ";
        var expectedTrimmedName = "Theme With Spaces";
        var rules = new Dictionary<string, string>
        {
            { "--accent-color1", "#2A579A" }
        };
        var command = new CreateThemeCommand(themeName, rules);

        using var db = CreateDbContext();
        var handler = new CreateThemeCommandHandler(db);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result > 0);
        var savedEntity = await db.BlogTheme.FindAsync([result], TestContext.Current.CancellationToken);
        Assert.NotNull(savedEntity);
        Assert.Equal(expectedTrimmedName, savedEntity.ThemeName);
    }

    [Fact]
    public async Task HandleAsync_EmptyRulesDictionary_CreatesThemeWithEmptyJsonObject()
    {
        // Arrange
        var themeName = "Minimal Theme";
        var rules = new Dictionary<string, string>();
        var command = new CreateThemeCommand(themeName, rules);

        using var db = CreateDbContext();
        var handler = new CreateThemeCommandHandler(db);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result > 0);
        var savedEntity = await db.BlogTheme.FindAsync([result], TestContext.Current.CancellationToken);
        Assert.NotNull(savedEntity);
        Assert.Equal("{}", savedEntity.CssRules);
    }

}
