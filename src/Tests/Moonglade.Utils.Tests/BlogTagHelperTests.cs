namespace Moonglade.Utils.Tests;

public class BlogTagHelperTests
{
    #region TagNormalizationDictionary Tests

    [Fact]
    public void TagNormalizationDictionary_ContainsExpectedMappings()
    {
        // Act
        var dictionary = BlogTagHelper.TagNormalizationDictionary;

        // Assert
        Assert.NotNull(dictionary);
        Assert.Equal(4, dictionary.Count);
        Assert.Equal("-", dictionary["."]);
        Assert.Equal("-sharp", dictionary["#"]);
        Assert.Equal("-", dictionary[" "]);
        Assert.Equal("-plus", dictionary["+"]);
    }

    [Fact]
    public void TagNormalizationDictionary_ReturnsNewInstanceEachTime()
    {
        // Act
        var dictionary1 = BlogTagHelper.TagNormalizationDictionary;
        var dictionary2 = BlogTagHelper.TagNormalizationDictionary;

        // Assert
        Assert.NotSame(dictionary1, dictionary2);
        Assert.Equal(dictionary1.Count, dictionary2.Count);
    }

    #endregion

    #region NormalizeName Tests

    [Theory]
    [InlineData("csharp", "csharp")]
    [InlineData("C#", "c-sharp")]
    [InlineData("ASP.NET", "asp-net")]
    [InlineData("F#", "f-sharp")]
    [InlineData("C++", "c-plus-plus")]
    [InlineData("Entity Framework", "entity-framework")]
    [InlineData("JAVA SCRIPT", "java-script")]
    [InlineData("Node.js", "node-js")]
    public void NormalizeName_WithEnglishTags_NormalizesCorrectly(string input, string expected)
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act
        var result = BlogTagHelper.NormalizeName(input, normalizations);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeName_WithDotNetSpecialCase_ReturnsDotNet()
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act
        var result1 = BlogTagHelper.NormalizeName(".NET", normalizations);
        var result2 = BlogTagHelper.NormalizeName(".net", normalizations);
        var result3 = BlogTagHelper.NormalizeName(".Net", normalizations);

        // Assert
        Assert.Equal("dot-net", result1);
        Assert.Equal("dot-net", result2);
        Assert.Equal("dot-net", result3);
    }

    [Fact]
    public void NormalizeName_WithChineseCharacters_ReturnsHexName()
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;
        const string chineseTag = "编程";

        // Act
        var result = BlogTagHelper.NormalizeName(chineseTag, normalizations);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("-", result);
        Assert.DoesNotContain("编程", result);

        // Should be hex representation
        var hexParts = result.Split('-');
        Assert.All(hexParts, part =>
        {
            Assert.True(part.All(c => "0123456789abcdef".Contains(c)),
                $"Part '{part}' contains non-hex characters");
        });
    }

    [Fact]
    public void NormalizeName_WithJapaneseCharacters_ReturnsHexName()
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;
        const string japaneseTag = "プログラミング";

        // Act
        var result = BlogTagHelper.NormalizeName(japaneseTag, normalizations);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("-", result);
        Assert.DoesNotContain("プログラミング", result);

        // Should be hex representation
        var hexParts = result.Split('-');
        Assert.All(hexParts, part =>
        {
            Assert.True(part.All(c => "0123456789abcdef".Contains(c)),
                $"Part '{part}' contains non-hex characters");
        });
    }

    [Fact]
    public void NormalizeName_WithMixedCharacters_ReturnsHexName()
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;
        const string mixedTag = "C#编程";

        // Act
        var result = BlogTagHelper.NormalizeName(mixedTag, normalizations);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("-", result);

        // Since it contains non-English characters, should return hex
        var hexParts = result.Split('-');
        Assert.All(hexParts, part =>
        {
            Assert.True(part.All(c => "0123456789abcdef".Contains(c)),
                $"Part '{part}' contains non-hex characters");
        });
    }

    [Fact]
    public void NormalizeName_WithEmptyNormalizations_KeepsOriginalForEnglish()
    {
        // Arrange
        var emptyNormalizations = new Dictionary<string, string>();

        // Act
        var result = BlogTagHelper.NormalizeName("simple", emptyNormalizations);

        // Assert
        Assert.Equal("simple", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeName_WithEmptyOrWhitespaceInput_HandlesGracefully(string input)
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act
        var result = BlogTagHelper.NormalizeName(input, normalizations);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void NormalizeName_WithNullNormalizations_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            BlogTagHelper.NormalizeName("test", null!));
    }

    [Fact]
    public void NormalizeName_WithNumbersAndSpecialChars_NormalizesCorrectly()
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act
        var result = BlogTagHelper.NormalizeName("HTML5 CSS3.0", normalizations);

        // Assert
        Assert.Equal("html5-css3-0", result);
    }

    #endregion

    #region IsValidTagName Tests

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
    public void IsValidTagName_WithValidEnglishTags_ReturnsTrue(string tagName, bool expected)
    {
        // Act
        var result = BlogTagHelper.IsValidTagName(tagName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("编程", true)]
    //[InlineData("プログラミング", true)]
    //[InlineData("개발", true)]
    [InlineData("程式設計", true)]
    [InlineData("软件开发", true)]
    //[InlineData("ソフトウェア", true)]
    public void IsValidTagName_WithValidCJKTags_ReturnsTrue(string tagName, bool expected)
    {
        // Act
        var result = BlogTagHelper.IsValidTagName(tagName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("\t", false)]
    [InlineData("\n", false)]
    public void IsValidTagName_WithNullOrWhitespace_ReturnsFalse(string tagName, bool expected)
    {
        // Act
        var result = BlogTagHelper.IsValidTagName(tagName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
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
    public void IsValidTagName_WithInvalidCharacters_ReturnsFalse(string tagName, bool expected)
    {
        // Act
        var result = BlogTagHelper.IsValidTagName(tagName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("مبرمج", false)] // Arabic
    [InlineData("программирование", false)] // Russian
    [InlineData("προγραμματισμός", false)] // Greek
    [InlineData("프로그래밍", false)] // Korean (mixed with non-CJK)
    [InlineData("😀emoji", false)] // Emoji
    [InlineData("🎉party", false)] // Emoji
    public void IsValidTagName_WithUnsupportedUnicodeCharacters_ReturnsFalse(string tagName, bool expected)
    {
        // Act
        var result = BlogTagHelper.IsValidTagName(tagName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidTagName_WithMixedEnglishAndCJK_ReturnsFalse()
    {
        // Arrange
        const string mixedTag = "C#编程";

        // Act
        var result = BlogTagHelper.IsValidTagName(mixedTag);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("a", true)] // Single character
    [InlineData("1", true)] // Single digit
    [InlineData(".", true)] // Single dot
    [InlineData("#", true)] // Single hash
    [InlineData("+", true)] // Single plus
    [InlineData("-", true)] // Single dash
    public void IsValidTagName_WithSingleValidCharacters_ReturnsTrue(string tagName, bool expected)
    {
        // Act
        var result = BlogTagHelper.IsValidTagName(tagName);

        // Assert
        Assert.Equal(expected, result);
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

    [Fact]
    public void IsValidTagName_PerformanceTest_ExecutesQuickly()
    {
        // Arrange
        const int iterations = 10000;
        const string testTag = "programming";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            BlogTagHelper.IsValidTagName(testTag);
        }
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Performance test took {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void NormalizeName_AndIsValidTagName_WorkTogether()
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;
        const string originalTag = "ASP.NET Core";

        // Act
        var isValid = BlogTagHelper.IsValidTagName(originalTag);
        var normalized = BlogTagHelper.NormalizeName(originalTag, normalizations);

        // Assert
        Assert.True(isValid);
        Assert.Equal("asp-net-core", normalized);
    }

    [Theory]
    [InlineData(".NET Framework", "dot-net")]
    [InlineData(".net core", "dot-net")]
    [InlineData(".Net Standard", "dot-net")]
    public void NormalizeName_DotNetVariations_AllReturnDotNet(string input, string expectedStart)
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act
        var result = BlogTagHelper.NormalizeName(input, normalizations);

        // Assert
        Assert.StartsWith(expectedStart, result);
    }

    [Fact]
    public void BlogTagHelper_WithRealWorldTags_HandlesCorrectly()
    {
        // Arrange
        var realWorldTags = new[]
        {
            "C#", "JavaScript", "ASP.NET Core", "Entity Framework",
            "HTML5", "CSS3", "Node.js", "React.js", "Vue.js",
            "programming", "web development", "software engineering"
        };
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act & Assert
        foreach (var tag in realWorldTags)
        {
            var isValid = BlogTagHelper.IsValidTagName(tag);
            Assert.True(isValid, $"Tag '{tag}' should be valid");

            var normalized = BlogTagHelper.NormalizeName(tag, normalizations);
            Assert.NotNull(normalized);
            Assert.DoesNotContain(" ", normalized);
            Assert.DoesNotContain(".", normalized);
            Assert.DoesNotContain("#", normalized);
            Assert.DoesNotContain("+", normalized);
        }
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void NormalizeName_WithNullInput_ThrowsException()
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BlogTagHelper.NormalizeName(null!, normalizations));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeName_WithEmptyInput_HandlesGracefully(string input)
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act
        var result = BlogTagHelper.NormalizeName(input, normalizations);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void IsValidTagName_CJKRegexPerformance_ExecutesQuickly()
    {
        // Arrange
        const int iterations = 1000;
        const string cjkTag = "编程开发";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            BlogTagHelper.IsValidTagName(cjkTag);
        }
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"CJK regex performance test took {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
    }

    [Fact]
    public void NormalizeName_WithEmptyDictionary_HandlesEnglishTagsCorrectly()
    {
        // Arrange
        var emptyDict = new Dictionary<string, string>();

        // Act
        var result = BlogTagHelper.NormalizeName("Simple Tag", emptyDict);

        // Assert
        Assert.Equal("simple tag", result);
    }

    [Fact]
    public void NormalizeName_HexConversion_IsConsistent()
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;
        const string unicodeTag = "测试";

        // Act
        var result1 = BlogTagHelper.NormalizeName(unicodeTag, normalizations);
        var result2 = BlogTagHelper.NormalizeName(unicodeTag, normalizations);

        // Assert
        Assert.Equal(result1, result2);
        Assert.NotEmpty(result1);
    }

    #endregion
}