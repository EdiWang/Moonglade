namespace Moonglade.Moderation.Tests;

public class LocalModerationServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidKeywords_CreatesInstance()
    {
        // Arrange
        var keywords = "badword1|badword2";

        // Act
        var service = new LocalModerationService(keywords);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithEmptyKeywords_CreatesInstance()
    {
        // Arrange
        var keywords = "";

        // Act
        var service = new LocalModerationService(keywords);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullKeywords_ThrowsException()
    {
        // Arrange
        string keywords = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocalModerationService(keywords));
    }

    #endregion

    #region ModerateContent Tests

    [Fact]
    public void ModerateContent_WithCleanInput_ReturnsOriginalInput()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        var input = "This is a clean sentence.";

        // Act
        var result = service.ModerateContent(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void ModerateContent_WithBadWords_ReturnsMaskedContent()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        var input = "This contains a badword in it.";

        // Act
        var result = service.ModerateContent(input);

        // Assert
        Assert.NotEqual(input, result);
        Assert.DoesNotContain("badword", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ModerateContent_WithEmptyOrWhitespaceInput_ReturnsInput(string input)
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);

        // Act
        var result = service.ModerateContent(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void ModerateContent_WithNullInput_ReturnsNull()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        string input = null;

        // Act
        var result = service.ModerateContent(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ModerateContent_WithMultipleBadWords_MasksAllBadWords()
    {
        // Arrange
        var keywords = "badword|offensive|inappropriate";
        var service = new LocalModerationService(keywords);
        var input = "This badword text is offensive and inappropriate.";

        // Act
        var result = service.ModerateContent(input);

        // Assert
        Assert.NotEqual(input, result);
        Assert.DoesNotContain("badword", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("offensive", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inappropriate", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ModerateContent_WithCaseInsensitiveBadWords_MasksRegardlessOfCase()
    {
        // Arrange
        var keywords = "badword";
        var service = new LocalModerationService(keywords);
        var input = "This BADWORD and BadWord should be masked.";

        // Act
        var result = service.ModerateContent(input);

        // Assert
        Assert.NotEqual(input, result);
        Assert.DoesNotContain("BADWORD", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BadWord", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region HasBadWords Tests

    [Fact]
    public void HasBadWords_WithCleanInput_ReturnsFalse()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        var input = "This is a clean sentence.";

        // Act
        var result = service.HasBadWords(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasBadWords_WithBadWordInput_ReturnsTrue()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        var input = "This contains a badword.";

        // Act
        var result = service.HasBadWords(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasBadWords_WithMultipleCleanInputs_ReturnsFalse()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        var inputs = new[] { "Clean text 1", "Clean text 2", "Clean text 3" };

        // Act
        var result = service.HasBadWords(inputs);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasBadWords_WithMultipleInputsOneBad_ReturnsTrue()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        var inputs = new[] { "Clean text 1", "This has badword", "Clean text 3" };

        // Act
        var result = service.HasBadWords(inputs);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasBadWords_WithEmptyArray_ReturnsFalse()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        var inputs = Array.Empty<string>();

        // Act
        var result = service.HasBadWords(inputs);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasBadWords_WithNullArray_ReturnsFalse()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        string[] inputs = null;

        // Act
        var result = service.HasBadWords(inputs);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void HasBadWords_WithEmptyOrWhitespaceInputs_ReturnsFalse(string input)
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);

        // Act
        var result = service.HasBadWords(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasBadWords_WithNullStringInArray_ReturnsFalse()
    {
        // Arrange
        var keywords = "badword|offensive";
        var service = new LocalModerationService(keywords);
        var inputs = new string[] { null, "", "clean text" };

        // Act
        var result = service.HasBadWords(inputs);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasBadWords_WithCaseInsensitiveBadWords_ReturnsTrue()
    {
        // Arrange
        var keywords = "badword";
        var service = new LocalModerationService(keywords);
        var input = "This has BADWORD in caps.";

        // Act
        var result = service.HasBadWords(input);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Integration Tests

    [Theory]
    [InlineData("fuck|shit|damn", "This is fucking terrible shit!", true)]
    [InlineData("spam|scam|phishing", "This is a legitimate email.", false)]
    [InlineData("", "Any content here", false)]
    [InlineData("test", "This is a test message", true)]
    public void IntegrationTest_ModerateAndDetect_WorksTogether(string keywords, string input, bool expectedHasBadWords)
    {
        // Arrange
        var service = new LocalModerationService(keywords);

        // Act
        var hasWords = service.HasBadWords(input);
        var moderated = service.ModerateContent(input);

        // Assert
        Assert.Equal(expectedHasBadWords, hasWords);

        if (expectedHasBadWords)
        {
            Assert.NotEqual(input, moderated);
        }
        else
        {
            Assert.Equal(input, moderated);
        }
    }

    #endregion
}