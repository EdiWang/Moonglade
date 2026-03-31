using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme.Tests;

public class DeleteThemeCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ThemeNotFound_ReturnsObjectNotFound()
    {
        // Arrange
        var command = new DeleteThemeCommand(999);
        using var db = CreateDbContext();
        var handler = new DeleteThemeCommandHandler(db);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.ObjectNotFound, result);
    }

    [Fact]
    public async Task HandleAsync_SystemTheme_ReturnsCanceled()
    {
        // Arrange
        using var db = CreateDbContext();
        var systemTheme = new BlogThemeEntity
        {
            Id = 1,
            ThemeName = "System Theme",
            ThemeType = ThemeType.System,
            CssRules = "{}"
        };
        db.BlogTheme.Add(systemTheme);
        await db.SaveChangesAsync();

        var command = new DeleteThemeCommand(1);
        var handler = new DeleteThemeCommandHandler(db);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Canceled, result);
        Assert.NotNull(await db.BlogTheme.FindAsync(1)); // Still exists
    }

    [Fact]
    public async Task HandleAsync_UserTheme_DeletesAndReturnsDone()
    {
        // Arrange
        using var db = CreateDbContext();
        var userTheme = new BlogThemeEntity
        {
            Id = 2,
            ThemeName = "Custom User Theme",
            ThemeType = ThemeType.User,
            CssRules = """{"--primary-color": "#ff0000"}"""
        };
        db.BlogTheme.Add(userTheme);
        await db.SaveChangesAsync();

        var command = new DeleteThemeCommand(2);
        var handler = new DeleteThemeCommandHandler(db);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Done, result);
        Assert.Null(await db.BlogTheme.FindAsync(2)); // Deleted
    }

    [Fact]
    public async Task HandleAsync_UserThemeWithAdditionalProps_DeletesAndReturnsDone()
    {
        // Arrange
        using var db = CreateDbContext();
        var userTheme = new BlogThemeEntity
        {
            Id = 5,
            ThemeName = "Feature Rich Theme",
            ThemeType = ThemeType.User,
            CssRules = """{"--accent-color": "#00ff00"}""",
            AdditionalProps = """{"darkMode": true}"""
        };
        db.BlogTheme.Add(userTheme);
        await db.SaveChangesAsync();

        var command = new DeleteThemeCommand(5);
        var handler = new DeleteThemeCommandHandler(db);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Done, result);
        Assert.Null(await db.BlogTheme.FindAsync(5)); // Deleted
    }

    [Fact]
    public async Task HandleAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogTheme.Add(new BlogThemeEntity
        {
            Id = 1,
            ThemeName = "Theme",
            ThemeType = ThemeType.User,
            CssRules = "{}"
        });
        await db.SaveChangesAsync();

        var command = new DeleteThemeCommand(1);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var handler = new DeleteThemeCommandHandler(db);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(command, cts.Token));
    }
}
