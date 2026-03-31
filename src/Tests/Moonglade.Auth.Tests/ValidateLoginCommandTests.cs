using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moq;

namespace Moonglade.Auth.Tests;

public class ValidateLoginCommandTests
{
    private readonly Mock<IBlogConfig> _blogConfigMock;
    private readonly Mock<ILogger<ValidateLoginCommandHandler>> _loggerMock;
    private readonly ValidateLoginCommandHandler _handler;

    public ValidateLoginCommandTests()
    {
        _blogConfigMock = new Mock<IBlogConfig>();
        _loggerMock = new Mock<ILogger<ValidateLoginCommandHandler>>();
        _handler = new ValidateLoginCommandHandler(_blogConfigMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_NullAccount_ReturnsFalse()
    {
        _blogConfigMock.Setup(c => c.LocalAccountSettings).Returns((LocalAccountSettings)null);

        var command = new ValidateLoginCommand("admin", "password");
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_WrongUsername_ReturnsFalse()
    {
        _blogConfigMock.Setup(c => c.LocalAccountSettings).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("wronguser", "admin123");
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_CorrectUsernameWrongPassword_ReturnsFalse()
    {
        _blogConfigMock.Setup(c => c.LocalAccountSettings).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", "wrongpassword");
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_CorrectCredentials_ReturnsTrue()
    {
        // DefaultValue: Username = "admin", password = "admin123"
        _blogConfigMock.Setup(c => c.LocalAccountSettings).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", "admin123");
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_CorrectCredentialsWithWhitespace_ReturnsTrue()
    {
        _blogConfigMock.Setup(c => c.LocalAccountSettings).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", " admin123 ");
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_FailedLogin_LogsWarning()
    {
        _blogConfigMock.Setup(c => c.LocalAccountSettings).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", "wrongpassword");
        await _handler.HandleAsync(command, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulLogin_DoesNotLogWarning()
    {
        _blogConfigMock.Setup(c => c.LocalAccountSettings).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", "admin123");
        await _handler.HandleAsync(command, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }
}
