using Microsoft.Extensions.Logging;
using Moq;

namespace Moonglade.Moderation.Tests;

public class MoongladeModeratorServiceTests
{
    private readonly Mock<ILogger<MoongladeModeratorService>> _mockLogger = new();
    private readonly Mock<ILocalModerationService> _mockLocalService = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_WithLocalService_InitializesSuccessfully()
    {
        // Act
        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullLocalService_LogsErrorAndDisablesService()
    {
        // Act
        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            null);

        // Assert
        Assert.NotNull(service);
        VerifyErrorLogging("Local moderation service is not configured");
    }

    #endregion

    #region Mask Method Tests

    [Fact]
    public async Task Mask_WhenServiceDisabled_ReturnsOriginalInput()
    {
        // Arrange
        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            null);

        const string input = "test input";

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task Mask_WithNullOrWhitespaceInput_ReturnsInput(string input)
    {
        // Arrange
        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(input, result);
        _mockLocalService.Verify(x => x.ModerateContent(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Mask_CallsLocalService()
    {
        // Arrange
        const string input = "test input with badword";
        const string maskedOutput = "test input with ****";
        _mockLocalService.Setup(x => x.ModerateContent(input)).Returns(maskedOutput);

        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(maskedOutput, result);
        _mockLocalService.Verify(x => x.ModerateContent(input), Times.Once);
    }

    #endregion

    #region Detect Method Tests

    [Fact]
    public async Task Detect_WhenServiceDisabled_ReturnsFalse()
    {
        // Arrange
        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            null);

        // Act
        var result = await service.Detect("test input");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Detect_WithNullInput_ReturnsFalse()
    {
        // Arrange
        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Detect_WithEmptyInput_ReturnsFalse()
    {
        // Arrange
        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect();

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", "   ", "\t", "\n")]
    [InlineData("   ", "", "\t")]
    [InlineData("\t", "\n", "")]
    public async Task Detect_WithOnlyWhitespaceInputs_ReturnsFalse(params string[] inputs)
    {
        // Arrange
        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.False(result);
        _mockLocalService.Verify(x => x.HasBadWords(It.IsAny<string[]>()), Times.Never);
    }

    [Fact]
    public async Task Detect_CallsLocalService()
    {
        // Arrange
        var inputs = new[] { "test input", "another input" };
        _mockLocalService.Setup(x => x.HasBadWords(inputs)).Returns(true);

        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.True(result);
        _mockLocalService.Verify(x => x.HasBadWords(inputs), Times.Once);
    }

    [Fact]
    public async Task Detect_WithMixedValidAndInvalidInputs_FiltersValidInputs()
    {
        // Arrange
        var inputs = new[] { "valid input", "", "   ", "another valid", "\t", "third valid" };
        var expectedValidInputs = new[] { "valid input", "another valid", "third valid" };

        _mockLocalService.Setup(x => x.HasBadWords(expectedValidInputs)).Returns(false);

        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.False(result);
        _mockLocalService.Verify(x => x.HasBadWords(expectedValidInputs), Times.Once);
    }

    #endregion

    #region Interface Compliance Tests

    [Fact]
    public void MoongladeModeratorService_ImplementsIModeratorService()
    {
        // Act
        var service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Assert
        Assert.IsAssignableFrom<IModeratorService>(service);
    }

    [Fact]
    public async Task IModeratorService_MaskMethod_WorksCorrectly()
    {
        // Arrange
        const string input = "test input";
        const string expectedOutput = "masked output";
        _mockLocalService.Setup(x => x.ModerateContent(input)).Returns(expectedOutput);

        IModeratorService service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(expectedOutput, result);
    }

    [Fact]
    public async Task IModeratorService_DetectMethod_WorksCorrectly()
    {
        // Arrange
        var inputs = new[] { "test input" };
        _mockLocalService.Setup(x => x.HasBadWords(inputs)).Returns(true);

        IModeratorService service = new MoongladeModeratorService(
            _mockLogger.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.True(result);
    }

    #endregion

    private void VerifyErrorLogging(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }
}
