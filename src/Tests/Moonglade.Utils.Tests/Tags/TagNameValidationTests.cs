namespace Moonglade.Utils.Tests.Tags;

public class TagNameValidationTests
{
    [Theory]
    [InlineData("csharp", true)]
    [InlineData("C#", true)]
    [InlineData("ASP.NET", true)]
    [InlineData("javascript", true)]
    [InlineData("HTML5", true)]
    [InlineData("CSS 3", true)]
    [InlineData("Node.js", true)]
    [InlineData("C++", true)]
    [InlineData("F#", true)]
    [InlineData("programming123", true)]
    [InlineData("web-dev", true)]
    [InlineData("tag with spaces", true)]
    [InlineData("UPPERCASE", true)]
    [InlineData("lowercase", true)]
    [InlineData("Mixed-Case", true)]
    [InlineData("编程", true)]
    [InlineData("程式設計", true)]
    [InlineData("软件开发", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("\t", false)]
    [InlineData("\n", false)]
    [InlineData("tag@symbol", false)]
    [InlineData("tag$money", false)]
    [InlineData("tag%percent", false)]
    [InlineData("tag^caret", false)]
    [InlineData("tag&amp", false)]
    [InlineData("tag*star", false)]
    [InlineData("tag(paren", false)]
    [InlineData("tag)paren", false)]
    [InlineData("tag=equals", false)]
    [InlineData("tag[bracket", false)]
    [InlineData("tag]bracket", false)]
    [InlineData("tag{brace", false)]
    [InlineData("tag}brace", false)]
    [InlineData("tag|pipe", false)]
    [InlineData("tag\\backslash", false)]
    [InlineData("tag:colon", false)]
    [InlineData("tag;semicolon", false)]
    [InlineData("tag\"quote", false)]
    [InlineData("tag'apostrophe", false)]
    [InlineData("tag<less", false)]
    [InlineData("tag>greater", false)]
    [InlineData("tag?question", false)]
    [InlineData("tag/slash", false)]
    [InlineData("tag,comma", false)]
    [InlineData("tag~tilde", false)]
    [InlineData("tag`backtick", false)]
    [InlineData("مبرمج", false)] // Arabic
    [InlineData("программирование", false)] // Russian
    [InlineData("προγραμματισμός", false)] // Greek
    [InlineData("프로그래밍", false)] // Korean (mixed with non-CJK)
    [InlineData("😀emoji", false)] // Emoji
    [InlineData("🎉party", false)] // Emoji
    [InlineData("C#编程", false)]
    [InlineData("a", true)] // Single character
    [InlineData("1", true)] // Single digit
    [InlineData(".", true)] // Single dot
    [InlineData("#", true)] // Single hash
    [InlineData("+", true)] // Single plus
    [InlineData("-", true)] // Single dash
    public void IsValidTagName_AcceptsOnlySupportedNames(string tagName, bool expected)
    {
        Assert.Equal(expected, BlogTagHelper.IsValidTagName(tagName!));
    }

    [Fact]
    public void IsValidTagName_WithVeryLongValidTag_ReturnsTrue()
    {
        // Arrange
        var longTag = new string('a', 1000);

        // Act
        var result = BlogTagHelper.IsValidTagName(longTag);

        // Assert
        Assert.True(result);
    }
}
