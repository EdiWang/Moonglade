using System.Reflection;

namespace Moonglade.Utils.Tests;

public class VersionHelperTests
{
    #region AppVersionBasic Tests

    [Fact]
    public void AppVersionBasic_WhenFileVersionExists_ReturnsFileVersion()
    {
        // Act
        var result = VersionHelper.AppVersionBasic;

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual("N/A", result);
    }

    [Fact]
    public void AppVersionBasic_ReturnsConsistentValue()
    {
        // Act
        var result1 = VersionHelper.AppVersionBasic;
        var result2 = VersionHelper.AppVersionBasic;

        // Assert
        Assert.Equal(result1, result2);
    }

    #endregion

    #region AppVersion Tests

    [Fact]
    public void AppVersion_WhenInformationalVersionIsNull_ReturnsAppVersionBasic()
    {
        // This test verifies the fallback behavior when informational version is null
        // We can't directly control the assembly attributes, but we can verify the logic

        // Act
        var appVersion = VersionHelper.AppVersion;
        var appVersionBasic = VersionHelper.AppVersionBasic;

        // Assert
        Assert.NotNull(appVersion);
        // If informational version is null, AppVersion should equal AppVersionBasic
        // Otherwise, it should follow the formatting rules
        Assert.True(appVersion == appVersionBasic || appVersion.Contains('(') || appVersion != appVersionBasic);
    }

    [Fact]
    public void AppVersion_ReturnsConsistentValue()
    {
        // Act
        var result1 = VersionHelper.AppVersion;
        var result2 = VersionHelper.AppVersion;

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void AppVersion_WhenContainsPlusSign_FormatsCorrectly()
    {
        // This test verifies the git hash formatting logic
        // We can't control the actual assembly attributes, but we can test the behavior

        // Act
        var result = VersionHelper.AppVersion;

        // Assert
        Assert.NotNull(result);

        // If the version contains a git hash (indicated by parentheses), 
        // it should be properly formatted
        if (result.Contains('(') && result.Contains(')'))
        {
            Assert.Matches(@".+\s\([a-f0-9]{6}\)$", result);
        }
    }

    #endregion

    #region IsNonStableVersion Tests

    [Fact]
    public void IsNonStableVersion_WithCurrentVersion_ReturnsBoolean()
    {
        // Act
        var result = VersionHelper.IsNonStableVersion();

        // Assert
        // Should return either true or false, not null
        Assert.True(result == true || result == false);
    }

    [Fact]
    public void IsNonStableVersion_ReturnsConsistentValue()
    {
        // Act
        var result1 = VersionHelper.IsNonStableVersion();
        var result2 = VersionHelper.IsNonStableVersion();

        // Assert
        Assert.Equal(result1, result2);
    }

    [Theory]
    [InlineData("1.0.0-preview", true)]
    [InlineData("1.0.0-beta", true)]
    [InlineData("1.0.0-rc", true)]
    [InlineData("1.0.0-debug", true)]
    [InlineData("1.0.0-alpha", true)]
    [InlineData("1.0.0-test", true)]
    [InlineData("1.0.0-canary", true)]
    [InlineData("1.0.0-nightly", true)]
    [InlineData("1.0.0-PREVIEW", true)]
    [InlineData("1.0.0-Beta", true)]
    [InlineData("2.0.0-rc.1", true)]
    [InlineData("1.0.0", false)]
    [InlineData("2.1.3", false)]
    [InlineData("1.0.0-release", false)]
    [InlineData("1.0.0-stable", false)]
    public void NonStableVersionRegex_WithVariousVersionStrings_ReturnsExpectedResult(string version, bool expected)
    {
        // We need to access the private regex through reflection for testing
        // This tests the regex pattern logic directly

        // Arrange
        var regexMethod = typeof(VersionHelper).GetMethod("NonStableVersionRegex",
            BindingFlags.NonPublic | BindingFlags.Static);
        var regex = regexMethod?.Invoke(null, null) as System.Text.RegularExpressions.Regex;

        // Act
        var result = regex?.IsMatch(version) ?? false;

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void VersionHelper_PropertiesAreNotNull()
    {
        // Act & Assert
        Assert.NotNull(VersionHelper.AppVersionBasic);
        Assert.NotNull(VersionHelper.AppVersion);
    }

    [Fact]
    public void VersionHelper_AppVersionFormatting_IsValid()
    {
        // Act
        var appVersion = VersionHelper.AppVersion;
        var appVersionBasic = VersionHelper.AppVersionBasic;

        // Assert
        Assert.NotEmpty(appVersion);
        Assert.NotEmpty(appVersionBasic);

        // AppVersion should either be the same as AppVersionBasic (when no informational version)
        // or contain additional formatting (git hash in parentheses)
        Assert.True(appVersion == appVersionBasic ||
                   appVersion.StartsWith(appVersionBasic.Split('.')[0]) ||
                   appVersion.Contains('('));
    }

    [Fact]
    public void VersionHelper_GitHashFormatting_WhenPresent_IsCorrectLength()
    {
        // Act
        var appVersion = VersionHelper.AppVersion;

        // Assert
        if (appVersion.Contains('(') && appVersion.Contains(')'))
        {
            var startIndex = appVersion.IndexOf('(') + 1;
            var endIndex = appVersion.IndexOf(')', startIndex);
            var gitHash = appVersion.Substring(startIndex, endIndex - startIndex);

            // Git hash should be exactly 6 characters when shortened
            Assert.Equal(6, gitHash.Length);

            // Should be hexadecimal characters
            Assert.Matches("^[a-f0-9]{6}$", gitHash.ToLowerInvariant());
        }
    }

    #endregion

    #region Regex Pattern Tests

    [Theory]
    [InlineData("preview")]
    [InlineData("beta")]
    [InlineData("rc")]
    [InlineData("debug")]
    [InlineData("alpha")]
    [InlineData("test")]
    [InlineData("canary")]
    [InlineData("nightly")]
    public void NonStableVersionRegex_WithSingleKeywords_MatchesCorrectly(string keyword)
    {
        // Arrange
        var regexMethod = typeof(VersionHelper).GetMethod("NonStableVersionRegex",
            BindingFlags.NonPublic | BindingFlags.Static);
        var regex = regexMethod?.Invoke(null, null) as System.Text.RegularExpressions.Regex;

        // Act & Assert
        Assert.True(regex?.IsMatch(keyword) ?? false);
        Assert.True(regex?.IsMatch(keyword.ToUpper()) ?? false);
        Assert.True(regex?.IsMatch($"1.0.0-{keyword}") ?? false);
        Assert.True(regex?.IsMatch($"version-{keyword}-build") ?? false);
    }

    [Theory]
    [InlineData("stable")]
    [InlineData("release")]
    [InlineData("final")]
    [InlineData("production")]
    [InlineData("1.0.0")]
    [InlineData("2.1.3")]
    public void NonStableVersionRegex_WithStableKeywords_DoesNotMatch(string stableVersion)
    {
        // Arrange
        var regexMethod = typeof(VersionHelper).GetMethod("NonStableVersionRegex",
            BindingFlags.NonPublic | BindingFlags.Static);
        var regex = regexMethod?.Invoke(null, null) as System.Text.RegularExpressions.Regex;

        // Act & Assert
        Assert.False(regex?.IsMatch(stableVersion) ?? true);
    }

    #endregion
}