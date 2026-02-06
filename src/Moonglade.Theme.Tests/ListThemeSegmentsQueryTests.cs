using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Theme.Tests;

public class ListThemeSegmentsQueryTests
{
    private readonly Mock<IRepositoryBase<BlogThemeEntity>> _mockRepo;
    private readonly ListThemeSegmentsQueryHandler _handler;

    public ListThemeSegmentsQueryTests()
    {
        _mockRepo = new Mock<IRepositoryBase<BlogThemeEntity>>();
        _handler = new ListThemeSegmentsQueryHandler(_mockRepo.Object);
    }

    #region HandleAsync Tests - Success Cases

    [Fact]
    public async Task HandleAsync_NoCustomThemes_ReturnsOnlySystemThemes()
    {
        // Arrange
        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BlogThemeEntity>());

        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Count); // 10 system themes
        Assert.All(result, theme => Assert.Equal(ThemeType.System, theme.ThemeType));
    }

    [Fact]
    public async Task HandleAsync_WithCustomThemes_ReturnsBothSystemAndCustomThemes()
    {
        // Arrange
        var customThemes = new List<BlogThemeEntity>
        {
            new()
            {
                Id = 1,
                ThemeName = "Custom Theme 1",
                CssRules = """{"--color": "blue"}""",
                ThemeType = ThemeType.User
            },
            new()
            {
                Id = 2,
                ThemeName = "Custom Theme 2",
                CssRules = """{"--color": "red"}""",
                ThemeType = ThemeType.User
            }
        };

        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customThemes);

        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

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
        var customThemes = new List<BlogThemeEntity>
        {
            new()
            {
                Id = 1,
                ThemeName = "Custom Theme",
                CssRules = """{"--color": "blue"}""",
                ThemeType = ThemeType.User
            }
        };

        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customThemes);

        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

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
        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BlogThemeEntity>());

        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        var expectedIds = new[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109 };
        var actualIds = result.Select(t => t.Id).ToArray();
        
        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public async Task HandleAsync_SystemThemesHaveValidProperties()
    {
        // Arrange
        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BlogThemeEntity>());

        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

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
        var customThemes = new List<BlogThemeEntity>
        {
            new()
            {
                Id = 50,
                ThemeName = "My Custom Theme",
                CssRules = """{"--primary": "#123456", "--secondary": "#abcdef"}""",
                AdditionalProps = """{"font": "Arial"}""",
                ThemeType = ThemeType.User
            }
        };

        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customThemes);

        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

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
        var customThemes = new List<BlogThemeEntity>
        {
            new() { Id = 1, ThemeName = "Theme 1", CssRules = "{}", ThemeType = ThemeType.User },
            new() { Id = 2, ThemeName = "Theme 2", CssRules = "{}", ThemeType = ThemeType.User },
            new() { Id = 3, ThemeName = "Theme 3", CssRules = "{}", ThemeType = ThemeType.User },
            new() { Id = 4, ThemeName = "Theme 4", CssRules = "{}", ThemeType = ThemeType.User },
            new() { Id = 5, ThemeName = "Theme 5", CssRules = "{}", ThemeType = ThemeType.User }
        };

        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customThemes);

        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(15, result.Count); // 10 system + 5 custom
        
        // Verify all custom themes are present
        for (int i = 1; i <= 5; i++)
        {
            Assert.Contains(result, t => t.Id == i && t.ThemeName == $"Theme {i}");
        }
    }

    [Fact]
    public async Task HandleAsync_CancellationToken_PassedToRepository()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockRepo.Setup(r => r.ListAsync(cts.Token))
            .ReturnsAsync(new List<BlogThemeEntity>());

        var query = new ListThemeSegmentsQuery();

        // Act
        await _handler.HandleAsync(query, cts.Token);

        // Assert
        _mockRepo.Verify(r => r.ListAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var customThemes = new List<BlogThemeEntity>
        {
            new() { Id = 1, ThemeName = "Custom", CssRules = "{}", ThemeType = ThemeType.User }
        };

        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customThemes);

        var query = new ListThemeSegmentsQuery();

        // Act
        var result1 = await _handler.HandleAsync(query, CancellationToken.None);
        var result2 = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(result1.Count, result2.Count);
        Assert.Equal(result1.Select(t => t.Id), result2.Select(t => t.Id));
    }

    #endregion

    #region HandleAsync Tests - Verification

    [Fact]
    public async Task HandleAsync_AlwaysCallsRepository()
    {
        // Arrange
        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BlogThemeEntity>());

        var query = new ListThemeSegmentsQuery();

        // Act
        await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNewListInstance()
    {
        // Arrange
        var customThemes = new List<BlogThemeEntity>
        {
            new() { Id = 1, ThemeName = "Custom", CssRules = "{}", ThemeType = ThemeType.User }
        };

        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customThemes);

        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        // The result should be a new list, not the same instance as customThemes
        Assert.NotSame(customThemes, result);
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
        var customThemes = Enumerable.Range(1, customCount)
            .Select(i => new BlogThemeEntity
            {
                Id = i,
                ThemeName = $"Theme {i}",
                CssRules = "{}",
                ThemeType = ThemeType.User
            })
            .ToList();

        _mockRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customThemes);

        var query = new ListThemeSegmentsQuery();

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(10 + customCount, result.Count);
    }

    #endregion
}
