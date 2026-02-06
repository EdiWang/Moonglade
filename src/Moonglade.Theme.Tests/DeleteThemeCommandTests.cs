using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Theme.Tests;

public class DeleteThemeCommandTests
{
    private readonly Mock<IRepositoryBase<BlogThemeEntity>> _mockRepo;
    private readonly DeleteThemeCommandHandler _handler;

    public DeleteThemeCommandTests()
    {
        _mockRepo = new Mock<IRepositoryBase<BlogThemeEntity>>();
        _handler = new DeleteThemeCommandHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_ThemeNotFound_ReturnsObjectNotFound()
    {
        // Arrange
        var command = new DeleteThemeCommand(999);
        _mockRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlogThemeEntity)null);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.ObjectNotFound, result);
        _mockRepo.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<BlogThemeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SystemTheme_ReturnsCanceled()
    {
        // Arrange
        var systemTheme = new BlogThemeEntity
        {
            Id = 1,
            ThemeName = "System Theme",
            ThemeType = ThemeType.System,
            CssRules = "{}"
        };
        var command = new DeleteThemeCommand(1);
        _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemTheme);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Canceled, result);
        _mockRepo.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<BlogThemeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UserTheme_DeletesAndReturnsDone()
    {
        // Arrange
        var userTheme = new BlogThemeEntity
        {
            Id = 2,
            ThemeName = "Custom User Theme",
            ThemeType = ThemeType.User,
            CssRules = """{"--primary-color": "#ff0000"}"""
        };
        var command = new DeleteThemeCommand(2);
        _mockRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userTheme);
        _mockRepo.Setup(r => r.DeleteAsync(userTheme, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Done, result);
        _mockRepo.Verify(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.DeleteAsync(userTheme, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UserThemeWithAdditionalProps_DeletesAndReturnsDone()
    {
        // Arrange
        var userTheme = new BlogThemeEntity
        {
            Id = 5,
            ThemeName = "Feature Rich Theme",
            ThemeType = ThemeType.User,
            CssRules = """{"--accent-color": "#00ff00"}""",
            AdditionalProps = """{"darkMode": true}"""
        };
        var command = new DeleteThemeCommand(5);
        _mockRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userTheme);
        _mockRepo.Setup(r => r.DeleteAsync(userTheme, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Done, result);
        _mockRepo.Verify(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.DeleteAsync(userTheme, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var command = new DeleteThemeCommand(1);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.HandleAsync(command, cts.Token));
    }
}
