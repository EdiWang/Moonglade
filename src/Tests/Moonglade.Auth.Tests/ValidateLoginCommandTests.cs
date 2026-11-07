using Microsoft.Extensions.Logging;
using Moq;

namespace Moonglade.Auth.Tests;

public class ValidateLoginCommandTests
{
    private readonly Mock<ILocalAccountReader> _accountsMock;
    private readonly Mock<ILogger<ValidateLoginCommandHandler>> _loggerMock;
    private readonly ValidateLoginCommandHandler _handler;

    public ValidateLoginCommandTests()
    {
        _accountsMock = new Mock<ILocalAccountReader>();
        _loggerMock = new Mock<ILogger<ValidateLoginCommandHandler>>();
        _handler = new ValidateLoginCommandHandler(_accountsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_NullAccount_ReturnsFalse()
    {
        _accountsMock.Setup(c => c.Read()).Returns((LocalAccountSettings)null!);

        var command = new ValidateLoginCommand("admin", "password");
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_WrongUsername_ReturnsFalse()
    {
        _accountsMock.Setup(c => c.Read()).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("wronguser", "admin123");
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_CorrectUsernameWrongPassword_ReturnsFalse()
    {
        _accountsMock.Setup(c => c.Read()).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", "wrongpassword");
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_CorrectCredentials_ReturnsTrue()
    {
        // DefaultValue: Username = "admin", password = "admin123"
        _accountsMock.Setup(c => c.Read()).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", "admin123");
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_CorrectCredentialsWithWhitespace_ReturnsTrue()
    {
        _accountsMock.Setup(c => c.Read()).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", " admin123 ");
        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_FailedLogin_LogsWarning()
    {
        _accountsMock.Setup(c => c.Read()).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", "wrongpassword");
        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

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
        _accountsMock.Setup(c => c.Read()).Returns(LocalAccountSettings.DefaultValue);

        var command = new ValidateLoginCommand("admin", "admin123");
        await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

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
