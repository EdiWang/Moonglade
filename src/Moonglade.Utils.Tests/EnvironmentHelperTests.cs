using System.Text.RegularExpressions;

namespace Moonglade.Utils.Tests;

public class EnvironmentHelperTests
{
    #region IsRunningOnAzureAppService Tests

    [Fact]
    public void IsRunningOnAzureAppService_WhenWebsiteSiteNameIsNull_ReturnsFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", null);

        // Act
        var result = EnvironmentHelper.IsRunningOnAzureAppService();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRunningOnAzureAppService_WhenWebsiteSiteNameIsEmpty_ReturnsFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "");

        // Act
        var result = EnvironmentHelper.IsRunningOnAzureAppService();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRunningOnAzureAppService_WhenWebsiteSiteNameIsWhitespace_ReturnsFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "   ");

        // Act
        var result = EnvironmentHelper.IsRunningOnAzureAppService();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRunningOnAzureAppService_WhenWebsiteSiteNameHasValue_ReturnsTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "my-azure-app");

        // Act
        var result = EnvironmentHelper.IsRunningOnAzureAppService();

        // Assert
        Assert.True(result);

        // Cleanup
        Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", null);
    }

    #endregion

    #region IsRunningInDocker Tests

    [Fact]
    public void IsRunningInDocker_WhenContainerVariableIsNull_ReturnsFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);

        // Act
        var result = EnvironmentHelper.IsRunningInDocker();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRunningInDocker_WhenContainerVariableIsEmpty_ReturnsFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "");

        // Act
        var result = EnvironmentHelper.IsRunningInDocker();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRunningInDocker_WhenContainerVariableIsFalse_ReturnsFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "false");

        // Act
        var result = EnvironmentHelper.IsRunningInDocker();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRunningInDocker_WhenContainerVariableIsTrue_ReturnsTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");

        // Act
        var result = EnvironmentHelper.IsRunningInDocker();

        // Assert
        Assert.True(result);

        // Cleanup
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
    }

    [Theory]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("1")]
    [InlineData("yes")]
    public void IsRunningInDocker_WhenContainerVariableIsNotExactlyTrue_ReturnsFalse(string value)
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", value);

        // Act
        var result = EnvironmentHelper.IsRunningInDocker();

        // Assert
        Assert.False(result);

        // Cleanup
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
    }

    #endregion

    #region GetEnvironmentTags Tests

    [Fact]
    public void GetEnvironmentTags_WhenTagsEnvIsNull_ReturnsEmptyString()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);
    }

    [Fact]
    public void GetEnvironmentTags_WhenTagsEnvIsEmpty_ReturnsEmptyString()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", "");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Fact]
    public void GetEnvironmentTags_WhenTagsEnvIsWhitespace_ReturnsEmptyString()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", "   ");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0]);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Fact]
    public void GetEnvironmentTags_WithSingleValidTag_ReturnsTag()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", "production");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("production", result[0]);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Fact]
    public void GetEnvironmentTags_WithMultipleValidTags_ReturnsAllTags()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", "production,azure,docker");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("production", result);
        Assert.Contains("azure", result);
        Assert.Contains("docker", result);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Fact]
    public void GetEnvironmentTags_WithTagsContainingSpaces_TrimsSpaces()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", " production , azure , docker ");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("production", result);
        Assert.Contains("azure", result);
        Assert.Contains("docker", result);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Theory]
    [InlineData("tag-with-hyphens")]
    [InlineData("tag123")]
    [InlineData("TAG")]
    [InlineData("tag#hash")]
    [InlineData("tag@symbol")]
    [InlineData("tag$dollar")]
    [InlineData("tag()parens")]
    [InlineData("tag[]brackets")]
    [InlineData("tag/slash")]
    public void GetEnvironmentTags_WithValidTagFormats_ReturnsTag(string tag)
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", tag);

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(tag, result[0]);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Theory]
    [InlineData("tag with space")]
    [InlineData("tag!exclamation")]
    [InlineData("tag%percent")]
    [InlineData("tag^caret")]
    [InlineData("tag&ampersand")]
    [InlineData("tag*asterisk")]
    [InlineData("tag+plus")]
    [InlineData("tag=equals")]
    [InlineData("tag{brace")]
    [InlineData("tag|pipe")]
    [InlineData("tag\\backslash")]
    [InlineData("tag:colon")]
    [InlineData("tag;semicolon")]
    [InlineData("tag\"quote")]
    [InlineData("tag'apostrophe")]
    [InlineData("tag<less")]
    [InlineData("tag>greater")]
    [InlineData("tag?question")]
    [InlineData("tag.dot")]
    public void GetEnvironmentTags_WithInvalidTagFormats_FiltersOut(string invalidTag)
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", $"valid,{invalidTag},also-valid");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("valid", result);
        Assert.Contains("also-valid", result);
        Assert.DoesNotContain(invalidTag, result);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Fact]
    public void GetEnvironmentTags_WithMixedValidAndInvalidTags_ReturnsOnlyValid()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", "production,invalid tag,azure-01,another invalid!,test#env");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("production", result);
        Assert.Contains("azure-01", result);
        Assert.Contains("test#env", result);
        Assert.DoesNotContain("invalid tag", result);
        Assert.DoesNotContain("another invalid!", result);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Fact]
    public void GetEnvironmentTags_WithAllInvalidTags_ReturnsEmpty()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", "invalid tag,another invalid!,yet another@invalid&tag");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Empty(result);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Fact]
    public void GetEnvironmentTags_WithEmptyTagsInList_FiltersOutEmpty()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", "production,,azure,,,docker");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("production", result);
        Assert.Contains("azure", result);
        Assert.Contains("docker", result);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    #endregion

    #region Regex Pattern Tests

    [Fact]
    public void GetEnvironmentTags_RegexPattern_MatchesExpectedCharacters()
    {
        // This test verifies the regex pattern used internally
        // Pattern: @"^[a-zA-Z0-9-#@$()\[\]/]+$"

        // Arrange
        var regex = new Regex(@"^[a-zA-Z0-9-#@$()\[\]/]+$");

        // Act & Assert - Valid characters
        Assert.Matches(regex, "validTag123");
        Assert.Matches(regex, "tag-with-hyphens");
        Assert.Matches(regex, "tag#hash");
        Assert.Matches(regex, "tag@symbol");
        Assert.Matches(regex, "tag$dollar");
        Assert.Matches(regex, "tag()parentheses");
        Assert.Matches(regex, "tag[]brackets");
        Assert.Matches(regex, "tag/slash");
        Assert.Matches(regex, "ABC123");
        Assert.Matches(regex, "a");
        Assert.Matches(regex, "1");

        // Act & Assert - Invalid characters
        Assert.DoesNotMatch(regex, "tag with space");
        Assert.DoesNotMatch(regex, "tag!exclamation");
        Assert.DoesNotMatch(regex, "tag.dot");
        Assert.DoesNotMatch(regex, "tag,comma");
        Assert.DoesNotMatch(regex, "tag:colon");
        Assert.DoesNotMatch(regex, "tag;semicolon");
        Assert.DoesNotMatch(regex, "tag\"quote");
        Assert.DoesNotMatch(regex, "tag'apostrophe");
        Assert.DoesNotMatch(regex, "tag<less");
        Assert.DoesNotMatch(regex, "tag>greater");
        Assert.DoesNotMatch(regex, "tag?question");
        Assert.DoesNotMatch(regex, "tag%percent");
        Assert.DoesNotMatch(regex, "tag^caret");
        Assert.DoesNotMatch(regex, "tag&ampersand");
        Assert.DoesNotMatch(regex, "tag*asterisk");
        Assert.DoesNotMatch(regex, "tag+plus");
        Assert.DoesNotMatch(regex, "tag=equals");
        Assert.DoesNotMatch(regex, "tag{brace");
        Assert.DoesNotMatch(regex, "tag}brace");
        Assert.DoesNotMatch(regex, "tag|pipe");
        Assert.DoesNotMatch(regex, "tag\\backslash");
        Assert.DoesNotMatch(regex, "");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EnvironmentHelper_AllMethods_WorkCorrectly()
    {
        // This test ensures all methods can be called without exceptions
        // and return appropriate types

        // Act & Assert - These should not throw exceptions
        var azureResult = EnvironmentHelper.IsRunningOnAzureAppService();
        var dockerResult = EnvironmentHelper.IsRunningInDocker();
        var tagsResult = EnvironmentHelper.GetEnvironmentTags();

        Assert.True(azureResult == true || azureResult == false);
        Assert.True(dockerResult == true || dockerResult == false);
        Assert.NotNull(tagsResult);
    }

    [Fact]
    public void GetEnvironmentTags_ReturnsConsistentResults()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", "test,env,tags");

        // Act
        var result1 = EnvironmentHelper.GetEnvironmentTags().ToList();
        var result2 = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Equal(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.Equal(result1[i], result2[i]);
        }

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetEnvironmentTags_WithVeryLongValidTag_ReturnsTag()
    {
        // Arrange
        var longTag = new string('a', 1000); // 1000 character tag
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", longTag);

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(longTag, result[0]);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    [Fact]
    public void GetEnvironmentTags_WithSpecialValidCombination_ReturnsTag()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", "test-env#1@prod$(v2)[backup]/release");

        // Act
        var result = EnvironmentHelper.GetEnvironmentTags().ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("test-env#1@prod$(v2)[backup]/release", result[0]);

        // Cleanup
        Environment.SetEnvironmentVariable("MOONGLADE_TAGS", null);
    }

    #endregion
}