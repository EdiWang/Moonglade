namespace Moonglade.Moderation.Tests;

public class CapturedKeywordsTests
{
    [Fact]
    public void ExplicitKeywords_AvoidRecapturingProviderBetweenFields()
    {
        var service = new MoongladeModeratorService(new UnavailableProvider());

        Assert.DoesNotContain("badword", service.Mask("A badword", "badword"));
        Assert.True(service.Detect(["Reader", "A badword"], "badword"));
        Assert.False(service.Detect(["Reader", "A badword"], "different"));
    }

    private sealed class UnavailableProvider : IModerationKeywordProvider
    {
        public string GetKeywords() => throw new InvalidOperationException("The captured operation must not recapture keywords.");
    }
}
