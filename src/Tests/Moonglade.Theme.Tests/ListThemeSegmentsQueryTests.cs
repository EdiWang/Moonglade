using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme.Tests;

public class ListThemeSegmentsQueryTests
{
    [Fact]
    public async Task HandleAsync_NoCustomThemes_ReturnsOnlySystemThemes()
    {
        using var db = CreateDbContext();
        var handler = new ListThemeSegmentsQueryHandler(db);

        var result = await handler.HandleAsync(new ListThemeSegmentsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(Enumerable.Range(100, 10), result.Select(theme => theme.Id));
        Assert.All(result, theme => Assert.Equal(ThemeType.System, theme.ThemeType));
    }

    [Fact]
    public async Task HandleAsync_CustomThemes_AppearAfterSystemThemesAndPreserveProperties()
    {
        using var db = CreateDbContext();
        var customThemes = new[]
        {
            new BlogThemeEntity
            {
                Id = 1,
                ThemeName = "Custom Blue",
                CssRules = """{"--primary": "#123456", "--secondary": "#abcdef"}""",
                AdditionalProps = """{"font": "Arial"}""",
                ThemeType = ThemeType.User
            },
            new BlogThemeEntity
            {
                Id = 2,
                ThemeName = "Custom Red",
                CssRules = """{"--primary": "red"}""",
                ThemeType = ThemeType.User
            }
        };
        db.BlogTheme.AddRange(customThemes);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        var handler = new ListThemeSegmentsQueryHandler(db);

        var result = await handler.HandleAsync(new ListThemeSegmentsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(12, result.Count);
        Assert.Equal(Enumerable.Range(100, 10), result.Take(10).Select(theme => theme.Id));
        Assert.All(result.Take(10), theme => Assert.Equal(ThemeType.System, theme.ThemeType));
        foreach (var expected in customThemes)
        {
            var actual = Assert.Single(result.Skip(10), theme => theme.Id == expected.Id);
            Assert.Equal(expected.ThemeName, actual.ThemeName);
            Assert.Equal(expected.CssRules, actual.CssRules);
            Assert.Equal(expected.AdditionalProps, actual.AdditionalProps);
            Assert.Equal(ThemeType.User, actual.ThemeType);
        }
    }

    private static BlogDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BlogDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
