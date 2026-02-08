using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using Moq;
using System.Text.Json;

namespace Moonglade.Theme.Tests;

public class CreateThemeCommandTests
{
    private readonly Mock<IRepositoryBase<BlogThemeEntity>> _mockRepo;
    private readonly CreateThemeCommandHandler _handler;

    public CreateThemeCommandTests()
    {
        _mockRepo = new Mock<IRepositoryBase<BlogThemeEntity>>();
        _handler = new CreateThemeCommandHandler(_mockRepo.Object);
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

        _mockRepo.Setup(r => r.AnyAsync(It.IsAny<ThemeByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(-1, result);
        _mockRepo.Verify(r => r.AnyAsync(It.IsAny<ThemeByNameSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<BlogThemeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NewTheme_CreatesThemeAndReturnsId()
    {
        // Arrange
        var themeName = "New Theme";
        var rules = new Dictionary<string, string>
        {
            { "--accent-color1", "#2A579A" },
            { "--accent-color2", "#FFFFFF" }
        };
        var command = new CreateThemeCommand(themeName, rules);

        _mockRepo.Setup(r => r.AnyAsync(It.IsAny<ThemeByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        BlogThemeEntity capturedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BlogThemeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<BlogThemeEntity, CancellationToken>((entity, ct) =>
            {
                capturedEntity = entity;
                entity.Id = 123; // Simulate database setting the Id
            })
            .ReturnsAsync((BlogThemeEntity entity, CancellationToken ct) => entity);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(123, result);
        _mockRepo.Verify(r => r.AnyAsync(It.IsAny<ThemeByNameSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<BlogThemeEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedEntity);
        Assert.Equal(themeName, capturedEntity.ThemeName);
        Assert.Equal(ThemeType.User, capturedEntity.ThemeType);

        var deserializedRules = JsonSerializer.Deserialize<Dictionary<string, string>>(capturedEntity.CssRules);
        Assert.NotNull(deserializedRules);
        Assert.Equal(rules.Count, deserializedRules.Count);
        Assert.Equal(rules["--accent-color1"], deserializedRules["--accent-color1"]);
        Assert.Equal(rules["--accent-color2"], deserializedRules["--accent-color2"]);
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

        _mockRepo.Setup(r => r.AnyAsync(It.IsAny<ThemeByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        BlogThemeEntity capturedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BlogThemeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<BlogThemeEntity, CancellationToken>((entity, ct) =>
            {
                capturedEntity = entity;
                entity.Id = 456;
            })
            .ReturnsAsync((BlogThemeEntity entity, CancellationToken ct) => entity);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(456, result);
        Assert.NotNull(capturedEntity);
        Assert.Equal(expectedTrimmedName, capturedEntity.ThemeName);
    }

    [Fact]
    public async Task HandleAsync_EmptyRulesDictionary_CreatesThemeWithEmptyJsonObject()
    {
        // Arrange
        var themeName = "Minimal Theme";
        var rules = new Dictionary<string, string>();
        var command = new CreateThemeCommand(themeName, rules);

        _mockRepo.Setup(r => r.AnyAsync(It.IsAny<ThemeByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        BlogThemeEntity capturedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BlogThemeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<BlogThemeEntity, CancellationToken>((entity, ct) =>
            {
                capturedEntity = entity;
                entity.Id = 789;
            })
            .ReturnsAsync((BlogThemeEntity entity, CancellationToken ct) => entity);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(789, result);
        Assert.NotNull(capturedEntity);
        Assert.Equal("{}", capturedEntity.CssRules);
    }

    [Fact]
    public async Task HandleAsync_MultipleRules_SerializesAllRulesCorrectly()
    {
        // Arrange
        var themeName = "Complex Theme";
        var rules = new Dictionary<string, string>
        {
            { "--accent-color1", "#2A579A" },
            { "--accent-color2", "#FFFFFF" },
            { "--accent-color3", "#000000" },
            { "--font-family", "Arial, sans-serif" },
            { "--border-radius", "5px" }
        };
        var command = new CreateThemeCommand(themeName, rules);

        _mockRepo.Setup(r => r.AnyAsync(It.IsAny<ThemeByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        BlogThemeEntity capturedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BlogThemeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<BlogThemeEntity, CancellationToken>((entity, ct) =>
            {
                capturedEntity = entity;
                entity.Id = 999;
            })
            .ReturnsAsync((BlogThemeEntity entity, CancellationToken ct) => entity);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(999, result);
        Assert.NotNull(capturedEntity);

        var deserializedRules = JsonSerializer.Deserialize<Dictionary<string, string>>(capturedEntity.CssRules);
        Assert.NotNull(deserializedRules);
        Assert.Equal(5, deserializedRules.Count);
        foreach (var kvp in rules)
        {
            Assert.True(deserializedRules.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, deserializedRules[kvp.Key]);
        }
    }

    [Fact]
    public async Task HandleAsync_CancellationTokenPassed_PassesToRepository()
    {
        // Arrange
        var themeName = "Test Theme";
        var rules = new Dictionary<string, string> { { "--color", "#123456" } };
        var command = new CreateThemeCommand(themeName, rules);
        var cts = new CancellationTokenSource();

        _mockRepo.Setup(r => r.AnyAsync(It.IsAny<ThemeByNameSpec>(), cts.Token))
            .ReturnsAsync(false);

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BlogThemeEntity>(), cts.Token))
            .Callback<BlogThemeEntity, CancellationToken>((entity, ct) => entity.Id = 111)
            .ReturnsAsync((BlogThemeEntity entity, CancellationToken ct) => entity);

        // Act
        var result = await _handler.HandleAsync(command, cts.Token);

        // Assert
        Assert.Equal(111, result);
        _mockRepo.Verify(r => r.AnyAsync(It.IsAny<ThemeByNameSpec>(), cts.Token), Times.Once);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<BlogThemeEntity>(), cts.Token), Times.Once);
    }
}
