namespace Moonglade.Moderation.Tests;

public class MoongladeModeratorServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Clean text")]
    public void Mask_ReturnsInputWithoutMatchingKeywords(string input)
    {
        var service = CreateService("badword");

        Assert.Equal(input, service.Mask(input));
    }

    [Fact]
    public void Mask_ReplacesMatchingKeywords()
    {
        var result = CreateService("badword|offensive").Mask("This badword is offensive.");

        Assert.DoesNotContain("badword", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("offensive", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("badword|offensive", "This contains a BADWORD.", true)]
    [InlineData("badword|offensive", "Clean text", false)]
    [InlineData("", "Any text", false)]
    [InlineData("badword", " ", false)]
    [InlineData("badword", null, false)]
    public void Detect_ReturnsWhetherInputContainsMatchingKeywords(string keywords, string input, bool expected)
    {
        Assert.Equal(expected, CreateService(keywords).Detect(input));
    }

    [Fact]
    public void UsesLatestKeywords()
    {
        var provider = new MutableKeywordProvider("alpha");
        var service = new MoongladeModeratorService(provider);

        Assert.True(service.Detect("alpha text"));
        provider.Keywords = "beta";
        Assert.False(service.Detect("alpha text"));
        Assert.True(service.Detect("beta text"));
    }

    private static MoongladeModeratorService CreateService(string keywords) =>
        new(new MutableKeywordProvider(keywords));

    private sealed class MutableKeywordProvider(string keywords) : IModerationKeywordProvider
    {
        public string Keywords { get; set; } = keywords;

        public string GetKeywords() => Keywords;
    }
}
