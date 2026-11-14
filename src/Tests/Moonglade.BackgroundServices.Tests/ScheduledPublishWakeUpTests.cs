namespace Moonglade.BackgroundServices.Tests;

public class ScheduledPublishWakeUpTests
{
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
