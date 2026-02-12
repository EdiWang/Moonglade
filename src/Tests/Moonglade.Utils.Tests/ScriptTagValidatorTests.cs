namespace Moonglade.Utils.Tests;

public class ScriptTagValidatorTests
{
    #region IsValidScriptBlock Tests

    [Fact]
    public void IsValidScriptBlock_WithNull_ReturnsTrue()
    {
        // Arrange
        string input = null;

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithEmptyString_ReturnsTrue()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithWhitespace_ReturnsTrue()
    {
        // Arrange
        const string input = "   \t\r\n   ";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithSingleScriptTag_ReturnsTrue()
    {
        // Arrange
        const string input = "<script>alert('Hello');</script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithMultipleScriptTags_ReturnsTrue()
    {
        // Arrange
        const string input = @"
            <script>console.log('First');</script>
            <script>console.log('Second');</script>
            <script>console.log('Third');</script>
        ";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithScriptTagAndSrcAttribute_ReturnsTrue()
    {
        // Arrange
        const string input = "<script src=\"https://example.com/script.js\"></script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithScriptTagAndMultipleAttributes_ReturnsTrue()
    {
        // Arrange
        const string input = "<script type=\"text/javascript\" src=\"https://example.com/script.js\" async defer></script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithScriptTagCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        const string input = "<SCRIPT>alert('test');</SCRIPT>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithScriptTagMixedCase_ReturnsTrue()
    {
        // Arrange
        const string input = "<ScRiPt>alert('test');</sCrIpT>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithMultilineScriptContent_ReturnsTrue()
    {
        // Arrange
        const string input = @"
<script>
    function test() {
        console.log('Line 1');
        console.log('Line 2');
        console.log('Line 3');
    }
    test();
</script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithScriptTagsAndWhitespace_ReturnsTrue()
    {
        // Arrange
        const string input = @"
            
            <script>console.log('test');</script>
            
            <script src=""https://example.com/lib.js""></script>
            
        ";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithTypeModuleAttribute_ReturnsTrue()
    {
        // Arrange
        const string input = "<script type=\"module\">import { test } from './module.js';</script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithEmptyScriptTag_ReturnsTrue()
    {
        // Arrange
        const string input = "<script></script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithNonScriptContent_ReturnsFalse()
    {
        // Arrange
        const string input = "<div>Hello World</div>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithPlainText_ReturnsFalse()
    {
        // Arrange
        const string input = "Just some plain text";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithScriptAndNonScriptContent_ReturnsFalse()
    {
        // Arrange
        const string input = @"
            <script>console.log('test');</script>
            <div>Invalid content</div>
        ";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithScriptAndPlainText_ReturnsFalse()
    {
        // Arrange
        const string input = @"
            <script>console.log('test');</script>
            Some invalid text
        ";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithIncompleteOpeningTag_ReturnsFalse()
    {
        // Arrange
        const string input = "<script>console.log('test');";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithIncompleteClosingTag_ReturnsFalse()
    {
        // Arrange
        const string input = "console.log('test');</script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithOtherHtmlTags_ReturnsFalse()
    {
        // Arrange
        const string input = "<style>body { color: red; }</style>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithMixedValidAndInvalidContent_ReturnsFalse()
    {
        // Arrange
        const string input = @"
            <p>Paragraph</p>
            <script>console.log('test');</script>
        ";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithScriptContentContainingHtmlLikeText_ReturnsTrue()
    {
        // Arrange
        const string input = "<script>var html = '<div>test</div>';</script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithCrossOriginAttribute_ReturnsTrue()
    {
        // Arrange
        const string input = "<script src=\"https://cdn.example.com/lib.js\" crossorigin=\"anonymous\" integrity=\"sha384-abc123\"></script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithNoopenerAttribute_ReturnsTrue()
    {
        // Arrange
        const string input = "<script src=\"https://example.com/script.js\" nomodule></script>";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithComplexRealWorldExample_ReturnsTrue()
    {
        // Arrange
        const string input = @"
<script async src=""https://www.googletagmanager.com/gtag/js?id=GA_MEASUREMENT_ID""></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', 'GA_MEASUREMENT_ID');
</script>
";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithScriptAndHtmlComment_ReturnsFalse()
    {
        // Arrange
        const string input = @"
<!-- Google Analytics -->
<script async src=""https://www.googletagmanager.com/gtag/js?id=GA_MEASUREMENT_ID""></script>
";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidScriptBlock_WithHtmlCommentOnly_ReturnsFalse()
    {
        // Arrange
        const string input = "<!-- This is a comment -->";

        // Act
        var result = ScriptTagValidator.IsValidScriptBlock(input);

        // Assert
        Assert.False(result);
    }

    #endregion
}
