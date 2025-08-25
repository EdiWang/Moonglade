using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Moonglade.Moderation.Tests;

public class MoongladeModeratorServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<MoongladeModeratorService>> _mockLogger;
    private readonly Mock<IOptions<ContentModeratorOptions>> _mockOptions;
    private readonly Mock<ILocalModerationService> _mockLocalService;
    private readonly Mock<IRemoteModerationService> _mockRemoteService;
    private readonly Mock<HttpContext> _mockHttpContext;

    public MoongladeModeratorServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<MoongladeModeratorService>>();
        _mockOptions = new Mock<IOptions<ContentModeratorOptions>>();
        _mockLocalService = new Mock<ILocalModerationService>();
        _mockRemoteService = new Mock<IRemoteModerationService>();
        _mockHttpContext = new Mock<HttpContext>();

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.TraceIdentifier).Returns("test-trace-id");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidConfiguration_InitializesSuccessfully()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Act
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockLocalService.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithLocalProviderAndNullLocalService_LogsErrorAndDisablesService()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Act
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null);

        // Assert
        VerifyErrorLogging("Local moderation service is not configured");
    }

    [Fact]
    public void Constructor_WithRemoteProviderAndInvalidConfiguration_LogsError()
    {
        // Arrange
        var options = new ContentModeratorOptions
        {
            Provider = "remote",
            ApiEndpoint = "",
            ApiKey = ""
        };
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Act
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null,
            null);

        // Assert
        VerifyErrorLogging("Remote ContentModerator API configuration is incomplete");
    }

    [Fact]
    public void Constructor_WithRemoteProviderAndNullRemoteService_LogsError()
    {
        // Arrange
        var options = new ContentModeratorOptions
        {
            Provider = "remote",
            ApiEndpoint = "https://api.example.com",
            ApiKey = "test-key"
        };
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Act
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null,
            null);

        // Assert
        VerifyErrorLogging("Remote moderation service is not configured");
    }

    #endregion

    #region Mask Method Tests

    [Fact]
    public async Task Mask_WhenServiceDisabled_ReturnsOriginalInput()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null); // No local service, so disabled

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
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task Mask_WithLocalProvider_CallsLocalService()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);
        
        const string input = "test input with badword";
        const string maskedOutput = "test input with ****";
        _mockLocalService.Setup(x => x.ModerateContent(input)).Returns(maskedOutput);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(maskedOutput, result);
        _mockLocalService.Verify(x => x.ModerateContent(input), Times.Once);
    }

    [Fact]
    public async Task Mask_WithLocalProviderAndNullLocalService_ReturnsOriginalInput()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null); // Null local service

        const string input = "test input";

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task Mask_WithRemoteProvider_CallsRemoteService()
    {
        // Arrange
        var options = CreateValidRemoteOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        const string input = "test input with badword";
        const string maskedOutput = "test input with ****";
        const string traceId = "test-trace-id";

        _mockRemoteService.Setup(x => x.MaskAsync(input, traceId))
                         .ReturnsAsync(maskedOutput);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null,
            _mockRemoteService.Object);

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(maskedOutput, result);
        _mockRemoteService.Verify(x => x.MaskAsync(input, traceId), Times.Once);
    }

    [Fact]
    public async Task Mask_WithRemoteProviderAndNullRemoteService_ReturnsOriginalInput()
    {
        // Arrange
        var options = CreateValidRemoteOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null,
            null); // Null remote service

        const string input = "test input";

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task Mask_WithNullHttpContext_UsesEmptyRequestId()
    {
        // Arrange
        var options = CreateValidRemoteOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);

        const string input = "test input";
        const string maskedOutput = "masked output";

        _mockRemoteService.Setup(x => x.MaskAsync(input, string.Empty))
                         .ReturnsAsync(maskedOutput);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null,
            _mockRemoteService.Object);

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(maskedOutput, result);
        _mockRemoteService.Verify(x => x.MaskAsync(input, string.Empty), Times.Once);
    }

    #endregion

    #region Detect Method Tests

    [Fact]
    public async Task Detect_WhenServiceDisabled_ReturnsFalse()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null); // No local service, so disabled

        // Act
        var result = await service.Detect("test input");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Detect_WithNullInput_ReturnsFalse()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
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
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
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
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Detect_WithLocalProvider_CallsLocalService()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        var inputs = new[] { "test input", "another input" };
        _mockLocalService.Setup(x => x.HasBadWords(inputs)).Returns(true);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.True(result);
        _mockLocalService.Verify(x => x.HasBadWords(inputs), Times.Once);
    }

    [Fact]
    public async Task Detect_WithLocalProviderAndNullLocalService_ReturnsFalse()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null); // Null local service

        // Act
        var result = await service.Detect("test input");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Detect_WithRemoteProvider_CallsRemoteService()
    {
        // Arrange
        var options = CreateValidRemoteOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        var inputs = new[] { "test input", "another input" };
        const string traceId = "test-trace-id";

        _mockRemoteService.Setup(x => x.DetectAsync(inputs, traceId))
                         .ReturnsAsync(true);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null,
            _mockRemoteService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.True(result);
        _mockRemoteService.Verify(x => x.DetectAsync(inputs, traceId), Times.Once);
    }

    [Fact]
    public async Task Detect_WithRemoteProviderAndNullRemoteService_ReturnsFalse()
    {
        // Arrange
        var options = CreateValidRemoteOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null,
            null); // Null remote service

        // Act
        var result = await service.Detect("test input");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Detect_WithMixedValidAndInvalidInputs_FiltersValidInputs()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        var inputs = new[] { "valid input", "", "   ", "another valid", "\t", "third valid" };
        var expectedValidInputs = new[] { "valid input", "another valid", "third valid" };

        _mockLocalService.Setup(x => x.HasBadWords(expectedValidInputs)).Returns(false);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.False(result);
        _mockLocalService.Verify(x => x.HasBadWords(expectedValidInputs), Times.Once);
    }

    [Fact]
    public async Task Detect_WithNullHttpContext_UsesEmptyRequestId()
    {
        // Arrange
        var options = CreateValidRemoteOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);

        var inputs = new[] { "test input" };
        _mockRemoteService.Setup(x => x.DetectAsync(inputs, string.Empty))
                         .ReturnsAsync(true);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null,
            _mockRemoteService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.True(result);
        _mockRemoteService.Verify(x => x.DetectAsync(inputs, string.Empty), Times.Once);
    }

    #endregion

    #region Interface Compliance Tests

    [Fact]
    public void MoongladeModeratorService_ImplementsIModeratorService()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Act
        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockLocalService.Object);

        // Assert
        Assert.IsAssignableFrom<IModeratorService>(service);
    }

    [Fact]
    public async Task IModeratorService_MaskMethod_WorksCorrectly()
    {
        // Arrange
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        const string input = "test input";
        const string expectedOutput = "masked output";
        _mockLocalService.Setup(x => x.ModerateContent(input)).Returns(expectedOutput);

        IModeratorService service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
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
        var options = CreateValidLocalOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        var inputs = new[] { "test input" };
        _mockLocalService.Setup(x => x.HasBadWords(inputs)).Returns(true);

        IModeratorService service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Detect(inputs);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Provider Detection Tests

    [Theory]
    [InlineData("local")]
    [InlineData("LOCAL")]
    [InlineData("Local")]
    [InlineData("lOcAl")]
    public async Task Service_WithLocalProviderVariations_UsesLocalService(string provider)
    {
        // Arrange
        var options = new ContentModeratorOptions { Provider = provider };
        _mockOptions.Setup(x => x.Value).Returns(options);

        const string input = "test input";
        const string maskedOutput = "masked output";
        _mockLocalService.Setup(x => x.ModerateContent(input)).Returns(maskedOutput);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockLocalService.Object);

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(maskedOutput, result);
        _mockLocalService.Verify(x => x.ModerateContent(input), Times.Once);
    }

    [Theory]
    [InlineData("remote")]
    [InlineData("azure")]
    [InlineData("api")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Service_WithNonLocalProvider_UsesRemoteService(string provider)
    {
        // Arrange
        var options = new ContentModeratorOptions
        {
            Provider = provider,
            ApiEndpoint = "https://api.example.com",
            ApiKey = "test-key"
        };
        _mockOptions.Setup(x => x.Value).Returns(options);

        const string input = "test input";
        const string maskedOutput = "masked output";
        const string traceId = "test-trace-id";

        _mockRemoteService.Setup(x => x.MaskAsync(input, traceId))
                         .ReturnsAsync(maskedOutput);

        var service = new MoongladeModeratorService(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            null,
            _mockRemoteService.Object);

        // Act
        var result = await service.Mask(input);

        // Assert
        Assert.Equal(maskedOutput, result);
        _mockRemoteService.Verify(x => x.MaskAsync(input, traceId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static ContentModeratorOptions CreateValidLocalOptions()
    {
        return new ContentModeratorOptions
        {
            Provider = "local",
            LocalKeywords = "badword|offensive"
        };
    }

    private static ContentModeratorOptions CreateValidRemoteOptions()
    {
        return new ContentModeratorOptions
        {
            Provider = "remote",
            ApiEndpoint = "https://api.example.com",
            ApiKey = "test-api-key",
            TimeoutSeconds = 30
        };
    }

    private void VerifyErrorLogging(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}