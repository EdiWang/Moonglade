using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Moonglade.Utils.Tests;

public class UrlHelperTests
{
    #region GetRouteLinkFromUrl Tests

    [Theory]
    [InlineData("https://example.com/post/2023/12/25/my-blog-post", "2023/12/25/my-blog-post")]
    [InlineData("http://blog.com/post/2022/1/1/new-year-post", "2022/1/1/new-year-post")]
    [InlineData("https://myblog.net/post/2024/6/15/summer-vacation", "2024/6/15/summer-vacation")]
    [InlineData("http://localhost:5000/post/2023/3/8/local-test", "2023/3/8/local-test")]
    [InlineData("https://subdomain.example.com/post/2023/12/31/year-end-summary", "2023/12/31/year-end-summary")]
    public void GetRouteLinkFromUrl_WithValidUrls_ReturnsCorrectRouteLink(string url, string expected)
    {
        // Act
        var result = UrlHelper.GetRouteLinkFromUrl(url);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://example.com/invalid/path")]
    [InlineData("https://example.com/post/")]
    [InlineData("https://example.com/post/2023")]
    [InlineData("https://example.com/post/2023/12")]
    [InlineData("https://example.com/post/2023/12/25")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/post/2023/12/25/test")]
    [InlineData("")]
    public void GetRouteLinkFromUrl_WithInvalidUrls_ThrowsFormatException(string url)
    {
        // Act & Assert
        var exception = Assert.Throws<FormatException>(() => UrlHelper.GetRouteLinkFromUrl(url));
        Assert.Equal("Invalid Slug Format", exception.Message);
    }

    [Theory]
    [InlineData("https://example.com/post/2023/13/25/invalid-month")]
    [InlineData("https://example.com/post/2023/12/32/invalid-day")]
    [InlineData("https://example.com/post/abcd/12/25/invalid-year")]
    public void GetRouteLinkFromUrl_WithInvalidDateComponents_ThrowsFormatException(string url)
    {
        // Act & Assert
        var exception = Assert.Throws<FormatException>(() => UrlHelper.GetRouteLinkFromUrl(url));
        Assert.Equal("Invalid Slug Format", exception.Message);
    }

    [Theory]
    [InlineData("https://example.com/post/2023/12/25/MY-BLOG-POST", "2023/12/25/my-blog-post")]
    [InlineData("https://example.com/post/2023/12/25/CamelCasePost", "2023/12/25/camelcasepost")]
    public void GetRouteLinkFromUrl_WithUpperCaseSlug_ReturnsLowerCaseResult(string url, string expected)
    {
        // Act
        var result = UrlHelper.GetRouteLinkFromUrl(url);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region GetUrlsFromContent Tests

    [Theory]
    [InlineData("<a href=\"https://example.com\">Link</a>", 1)]
    [InlineData("<a href='http://test.com'>Test</a>", 1)]
    [InlineData("<a href=\"https://example.com\">Link1</a> and <a href=\"http://test.com\">Link2</a>", 2)]
    [InlineData("No links here", 0)]
    public void GetUrlsFromContent_WithValidContent_ReturnsCorrectNumberOfUrls(string content, int expectedCount)
    {
        // Act
        var result = UrlHelper.GetUrlsFromContent(content);

        // Assert
        Assert.Equal(expectedCount, result.Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void GetUrlsFromContent_WithNullOrWhitespaceContent_ThrowsArgumentNullException(string content)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => UrlHelper.GetUrlsFromContent(content!));
    }

    [Fact]
    public void GetUrlsFromContent_WithValidUrls_ReturnsCorrectUris()
    {
        // Arrange
        const string content = "<a href=\"https://example.com\">Example</a> and <a href=\"http://test.com/path\">Test</a>";

        // Act
        var result = UrlHelper.GetUrlsFromContent(content).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, uri => uri.ToString() == "https://example.com/");
        Assert.Contains(result, uri => uri.ToString() == "http://test.com/path");
    }

    [Theory]
    [InlineData("<a href=\"invalid-url\">Invalid</a>")]
    [InlineData("<a href=\"javascript:alert('xss')\">Malicious</a>")]
    [InlineData("<a href=\"/relative/path\">Relative</a>")]
    [InlineData("<a href=\"#anchor\">Anchor</a>")]
    public void GetUrlsFromContent_WithInvalidUrls_FiltersOutInvalidOnes(string content)
    {
        // Act
        var result = UrlHelper.GetUrlsFromContent(content);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetUrlsFromContent_WithMixedValidAndInvalidUrls_ReturnsOnlyValidOnes()
    {
        // Arrange
        const string content = """
            <a href="https://valid.com">Valid</a>
            <a href="invalid-url">Invalid</a>
            <a href="http://another-valid.com">Another Valid</a>
            <a href="/relative">Relative</a>
            """;

        // Act
        var result = UrlHelper.GetUrlsFromContent(content).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, uri => uri.ToString() == "https://valid.com/");
        Assert.Contains(result, uri => uri.ToString() == "http://another-valid.com/");
    }

    [Theory]
    [InlineData("<A HREF=\"https://example.com\">Upper Case</A>")]
    [InlineData("<a HREF=\"https://example.com\">Mixed Case</a>")]
    [InlineData("<a href=\"https://example.com\" target=\"_blank\">With Attributes</a>")]
    public void GetUrlsFromContent_WithVariousHtmlFormats_ExtractsUrls(string content)
    {
        // Act
        var result = UrlHelper.GetUrlsFromContent(content);

        // Assert
        Assert.Single(result);
        Assert.Equal("https://example.com/", result.First().ToString());
    }

    #endregion

    #region GenerateRouteLink Tests

    [Theory]
    [InlineData("2023-12-25", "my-blog-post", "2023/12/25/my-blog-post")]
    [InlineData("2022-01-01", "new-year-post", "2022/1/1/new-year-post")]
    [InlineData("2024-06-15", "summer-vacation", "2024/6/15/summer-vacation")]
    [InlineData("2023-03-08", "test-post", "2023/3/8/test-post")]
    public void GenerateRouteLink_WithValidInputs_ReturnsCorrectRoute(string dateString, string slug, string expected)
    {
        // Arrange
        var publishDate = DateTime.Parse(dateString, CultureInfo.InvariantCulture);

        // Act
        var result = UrlHelper.GenerateRouteLink(publishDate, slug);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void GenerateRouteLink_WithNullOrWhitespaceSlug_ThrowsArgumentNullException(string slug)
    {
        // Arrange
        var publishDate = new DateTime(2023, 12, 25);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => UrlHelper.GenerateRouteLink(publishDate, slug!));
        Assert.Equal("slug", exception.ParamName);
        Assert.Contains("Slug must not be null or empty.", exception.Message);
    }

    [Theory]
    [InlineData("MY-BLOG-POST", "my-blog-post")]
    [InlineData("CamelCasePost", "camelcasepost")]
    [InlineData("UPPERCASE", "uppercase")]
    public void GenerateRouteLink_WithUpperCaseSlug_ReturnsLowerCaseSlug(string slug, string expectedSlug)
    {
        // Arrange
        var publishDate = new DateTime(2023, 12, 25);
        var expectedResult = $"2023/12/25/{expectedSlug}";

        // Act
        var result = UrlHelper.GenerateRouteLink(publishDate, slug);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GenerateRouteLink_WithMinDateTime_HandlesCorrectly()
    {
        // Arrange
        var publishDate = DateTime.MinValue;
        const string slug = "test-post";

        // Act
        var result = UrlHelper.GenerateRouteLink(publishDate, slug);

        // Assert
        Assert.Equal("0001/1/1/test-post", result);
    }

    [Fact]
    public void GenerateRouteLink_WithMaxDateTime_HandlesCorrectly()
    {
        // Arrange
        var publishDate = DateTime.MaxValue;
        const string slug = "test-post";

        // Act
        var result = UrlHelper.GenerateRouteLink(publishDate, slug);

        // Assert
        Assert.Equal("9999/12/31/test-post", result);
    }

    #endregion

    #region GetDNSPrefetchUrl Tests

    [Theory]
    [InlineData("https://cdn.example.com/assets", "https://cdn.example.com/")]
    [InlineData("http://cdn.test.com/images", "http://cdn.test.com/")]
    [InlineData("https://static.myblog.com:8080/content", "https://static.myblog.com:8080/")]
    [InlineData("http://localhost:3000", "http://localhost:3000/")]
    public void GetDNSPrefetchUrl_WithValidCdnEndpoints_ReturnsCorrectPrefetchUrl(string cdnEndpoint, string expected)
    {
        // Act
        var result = UrlHelper.GetDNSPrefetchUrl(cdnEndpoint);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void GetDNSPrefetchUrl_WithNullOrWhitespaceCdnEndpoint_ReturnsEmptyString(string cdnEndpoint)
    {
        // Act
        var result = UrlHelper.GetDNSPrefetchUrl(cdnEndpoint!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("not-a-url")]
    public void GetDNSPrefetchUrl_WithInvalidCdnEndpoint_ThrowsUriFormatException(string cdnEndpoint)
    {
        // Act & Assert
        Assert.Throws<UriFormatException>(() => UrlHelper.GetDNSPrefetchUrl(cdnEndpoint));
    }

    #endregion

    #region ResolveRootUrl Tests

    [Fact]
    public void ResolveRootUrl_WithNullContextAndPreferCanonicalFalse_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            UrlHelper.ResolveRootUrl(null!, "https://canonical.com", false));
        Assert.Equal("ctx", exception.ParamName);
        Assert.Contains("HttpContext must not be null when preferCanonical is 'false'", exception.Message);
    }

    [Fact]
    public void ResolveRootUrl_WithPreferCanonicalTrue_ReturnsCanonicalUrl()
    {
        // Arrange
        const string canonicalPrefix = "https://canonical.com";

        // Act
        var result = UrlHelper.ResolveRootUrl(null!, canonicalPrefix, true);

        // Assert
        Assert.Equal("https://canonical.com/", result);
    }

    [Fact]
    public void ResolveRootUrl_WithValidHttpContext_ReturnsRequestUrl()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com");

        // Act
        var result = UrlHelper.ResolveRootUrl(context, "https://canonical.com", false);

        // Assert
        Assert.Equal("https://example.com", result);
    }

    [Theory]
    [InlineData("https://example.com/", true, "https://example.com")]
    [InlineData("https://example.com", true, "https://example.com")]
    [InlineData("https://example.com/", false, "https://example.com/")]
    [InlineData("https://example.com", false, "https://example.com/")]
    public void ResolveRootUrl_WithRemoveTailSlashOption_HandlesTrailingSlashCorrectly(string canonicalPrefix, bool removeTailSlash, string expected)
    {
        // Act
        var result = UrlHelper.ResolveRootUrl(null!, canonicalPrefix, true, removeTailSlash);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region ResolveCanonicalUrl Tests

    [Theory]
    [InlineData("https://example.com", "", "https://example.com/")]
    [InlineData("https://example.com", "blog/post", "https://example.com/blog/post")]
    [InlineData("https://example.com/", "blog/post", "https://example.com/blog/post")]
    [InlineData("https://example.com", "/blog/post", "https://example.com/blog/post")]
    [InlineData("https://example.com:8080", "api/v1", "https://example.com:8080/api/v1")]
    public void ResolveCanonicalUrl_WithValidInputs_ReturnsCorrectUrl(string prefix, string path, string expected)
    {
        // Act
        var result = UrlHelper.ResolveCanonicalUrl(prefix, path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ResolveCanonicalUrl_WithNullOrWhitespacePrefix_ReturnsEmptyString(string prefix)
    {
        // Act
        var result = UrlHelper.ResolveCanonicalUrl(prefix!, "some/path");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolveCanonicalUrl_WithNullPath_HandlesGracefully()
    {
        // Act
        var result = UrlHelper.ResolveCanonicalUrl("https://example.com", null!);

        // Assert
        Assert.Equal("https://example.com/", result);
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert('xss')")]
    public void ResolveCanonicalUrl_WithInvalidPrefix_ThrowsUriFormatException(string prefix)
    {
        // Act & Assert
        var exception = Assert.Throws<UriFormatException>(() => UrlHelper.ResolveCanonicalUrl(prefix, "path"));
        Assert.Contains($"Prefix '{prefix}' is not a valid URL.", exception.Message);
    }

    [Theory]
    [InlineData("https://example.com", "invalid path with spaces")]
    [InlineData("https://example.com", "path with | invalid chars")]
    public void ResolveCanonicalUrl_WithInvalidPath_ReturnsEmptyString(string prefix, string path)
    {
        // Act
        var result = UrlHelper.ResolveCanonicalUrl(prefix, path);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void UrlHelper_AllMethods_HandleUnicodeCharactersCorrectly()
    {
        // Test GenerateRouteLink with unicode
        var unicodeSlug = "≤‚ ‘-post";
        var publishDate = new DateTime(2023, 12, 25);
        var routeResult = UrlHelper.GenerateRouteLink(publishDate, unicodeSlug);
        Assert.Equal("2023/12/25/≤‚ ‘-post", routeResult);

        // Test ResolveCanonicalUrl with unicode path
        var canonicalResult = UrlHelper.ResolveCanonicalUrl("https://example.com", "≤‚ ‘/path");
        Assert.Equal("https://example.com/≤‚ ‘/path", canonicalResult);
    }

    [Fact]
    public void UrlHelper_WithComplexHtmlContent_ExtractsAllValidUrls()
    {
        // Arrange
        const string complexHtml = """
            <div>
                <a href="https://example.com">Main Site</a>
                <p>Check out <a href="http://blog.example.com/post/1">this post</a></p>
                <span><a href="https://cdn.example.com/image.jpg">Image</a></span>
                <a href="/relative/link">Relative</a>
                <a href="mailto:test@example.com">Email</a>
                <a href="javascript:void(0)">JavaScript</a>
            </div>
            """;

        // Act
        var urls = UrlHelper.GetUrlsFromContent(complexHtml).ToList();

        // Assert
        Assert.Equal(3, urls.Count);
        Assert.Contains(urls, uri => uri.ToString() == "https://example.com/");
        Assert.Contains(urls, uri => uri.ToString() == "http://blog.example.com/post/1");
        Assert.Contains(urls, uri => uri.ToString() == "https://cdn.example.com/image.jpg");
    }

    [Fact]
    public void UrlHelper_RegexPerformance_HandlesLargeContent()
    {
        // Arrange - Create large content with many links
        var largeContent = string.Join("\n", 
            Enumerable.Range(1, 1000)
                .Select(i => $"<a href=\"https://example{i}.com\">Link {i}</a>"));

        // Act & Assert - Should complete without timeout
        var urls = UrlHelper.GetUrlsFromContent(largeContent);
        Assert.Equal(1000, urls.Count());
    }

    #endregion
}