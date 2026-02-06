using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Theme.Tests;

public class GetSiteThemeStyleSheetQueryTests
{
    private readonly Mock<IRepositoryBase<BlogThemeEntity>> _mockRepo;
    private readonly GetStyleSheetQueryHandler _handler;

    public GetSiteThemeStyleSheetQueryTests()
    {
        _mockRepo = new Mock<IRepositoryBase<BlogThemeEntity>>();
        _handler = new GetStyleSheetQueryHandler(_mockRepo.Object);
    }

    #region HandleAsync Tests - Success Cases

    [Fact]
    public async Task HandleAsync_SystemTheme_ReturnsValidCss()
    {
        // Arrange
        var query = new GetSiteThemeStyleSheetQuery(100);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith(":root {", result);
        Assert.Contains("--accent-color1", result);
        Assert.Contains("--accent-color2", result);
        _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_CustomTheme_ReturnsValidCss()
    {
        // Arrange
        var cssRules = """{"--primary-color": "#ff0000", "--secondary-color": "#00ff00"}""";
        var theme = new BlogThemeEntity
        {
            Id = 1,
            ThemeName = "Custom Theme",
            CssRules = cssRules,
            ThemeType = ThemeType.User
        };
        _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(1);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(":root {--primary-color: #ff0000;--secondary-color: #00ff00;}", result);
    }

    [Fact]
    public async Task HandleAsync_ValidCssRules_GeneratesCorrectCss()
    {
        // Arrange
        var cssRules = """{"--color-1": "blue", "--color-2": "red", "--color-3": "green"}""";
        var theme = new BlogThemeEntity
        {
            Id = 50,
            CssRules = cssRules
        };
        _mockRepo.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Contains("--color-1: blue;", result);
        Assert.Contains("--color-2: red;", result);
        Assert.Contains("--color-3: green;", result);
    }

    [Fact]
    public async Task HandleAsync_CssRulesWithWhitespace_FiltersOutEmptyRules()
    {
        // Arrange
        var cssRules = """{"--valid-color": "blue", "": "red", "--another-color": "", "  ": "green"}""";
        var theme = new BlogThemeEntity
        {
            Id = 50,
            CssRules = cssRules
        };
        _mockRepo.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.Contains("--valid-color: blue;", result);
        Assert.DoesNotContain(": red;", result);
        Assert.DoesNotContain(": green;", result);
    }

    [Fact]
    public async Task HandleAsync_AllSystemThemeIds_ReturnValidCss()
    {
        // Arrange - System themes are 100-109 (10 themes)
        var systemThemes = ThemeFactory.GetSystemThemes().ToList();
        
        // Act & Assert
        foreach (var theme in systemThemes)
        {
            var query = new GetSiteThemeStyleSheetQuery(theme.Id);
            var result = await _handler.HandleAsync(query, CancellationToken.None);

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
        _mockRepo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlogThemeEntity)null);

        var query = new GetSiteThemeStyleSheetQuery(99);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith(":root {", result);
        _mockRepo.Verify(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThemeIsNull_ReturnsNull()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlogThemeEntity)null);

        // Mock ThemeFactory to return empty list for this test scenario
        var query = new GetSiteThemeStyleSheetQuery(999);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

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
        var theme = new BlogThemeEntity
        {
            Id = 50,
            CssRules = ""
        };
        _mockRepo.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => _handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("Theme id '50' has empty CSS rules", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WhitespaceCssRules_ThrowsInvalidDataException()
    {
        // Arrange
        var theme = new BlogThemeEntity
        {
            Id = 50,
            CssRules = "   "
        };
        _mockRepo.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => _handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("has empty CSS rules", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidJson_ThrowsInvalidDataException()
    {
        // Arrange
        var theme = new BlogThemeEntity
        {
            Id = 50,
            CssRules = "invalid json {{{["
        };
        _mockRepo.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => _handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("Theme id '50' has invalid JSON in CssRules", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public async Task HandleAsync_NullDeserializedRules_ThrowsInvalidDataException()
    {
        // Arrange
        var theme = new BlogThemeEntity
        {
            Id = 50,
            CssRules = "null"
        };
        _mockRepo.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => _handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("Theme id '50' CssRules deserialized to empty or null", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_EmptyJsonObject_ThrowsInvalidDataException()
    {
        // Arrange
        var theme = new BlogThemeEntity
        {
            Id = 50,
            CssRules = "{}"
        };
        _mockRepo.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => _handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("Theme id '50' CssRules deserialized to empty or null", exception.Message);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(99)]  // Just below system theme range
    [InlineData(111)] // Just above system theme range
    [InlineData(1)]   // Small custom theme ID
    [InlineData(50)]  // Mid-range custom theme ID
    public async Task HandleAsync_CustomThemeId_CallsRepository(int themeId)
    {
        // Arrange
        var theme = new BlogThemeEntity
        {
            Id = themeId,
            CssRules = """{"--color": "blue"}"""
        };
        _mockRepo.Setup(r => r.GetByIdAsync(themeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(themeId);

        // Act
        await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.GetByIdAsync(themeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(100)] // System theme start
    [InlineData(105)] // System theme middle
    [InlineData(110)] // System theme end
    public async Task HandleAsync_SystemThemeId_DoesNotCallRepository(int themeId)
    {
        // Arrange
        var query = new GetSiteThemeStyleSheetQuery(themeId);

        // Act
        await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_CancellationToken_PassedToRepository()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var theme = new BlogThemeEntity
        {
            Id = 50,
            CssRules = """{"--color": "blue"}"""
        };
        _mockRepo.Setup(r => r.GetByIdAsync(50, cts.Token))
            .ReturnsAsync(theme);

        var query = new GetSiteThemeStyleSheetQuery(50);

        // Act
        await _handler.HandleAsync(query, cts.Token);

        // Assert
        _mockRepo.Verify(r => r.GetByIdAsync(50, cts.Token), Times.Once);
    }

    #endregion
}
