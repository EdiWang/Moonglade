namespace Moonglade.BackgroundServices.Tests;

public class ScheduledPublishWakeUpTests
{
    [Fact]
    public void GetWakeToken_ReturnsValidToken()
    {
        // Arrange
        var wakeUp = new ScheduledPublishWakeUp();

        // Act
        var token = wakeUp.GetWakeToken();

        // Assert
        Assert.False(token.IsCancellationRequested);
    }

    [Fact]
    public void WakeUp_CancelsToken()
    {
        // Arrange
        var wakeUp = new ScheduledPublishWakeUp();
        var token = wakeUp.GetWakeToken();

        // Act
        wakeUp.WakeUp();

        // Assert
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void GetWakeToken_AfterWakeUp_ReturnsNewToken()
    {
        // Arrange
        var wakeUp = new ScheduledPublishWakeUp();
        var firstToken = wakeUp.GetWakeToken();
        wakeUp.WakeUp();

        // Act
        var secondToken = wakeUp.GetWakeToken();

        // Assert
        Assert.True(firstToken.IsCancellationRequested);
        Assert.False(secondToken.IsCancellationRequested);
    }

    [Fact]
    public void WakeUp_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var wakeUp = new ScheduledPublishWakeUp();
        wakeUp.GetWakeToken();

        // Act & Assert
        wakeUp.WakeUp();
        wakeUp.WakeUp();
    }

    [Fact]
    public void GetWakeToken_CalledMultipleTimes_ReturnsSameToken()
    {
        // Arrange
        var wakeUp = new ScheduledPublishWakeUp();

        // Act
        var token1 = wakeUp.GetWakeToken();
        var token2 = wakeUp.GetWakeToken();

        // Assert
        Assert.Equal(token1, token2);
    }
}
