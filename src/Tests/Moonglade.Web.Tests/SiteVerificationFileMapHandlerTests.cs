using Edi.CacheAside.InMemory;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Features.SiteVerification;
using Moonglade.Web.Handlers;
using Moq;
using System.Text;

namespace Moonglade.Web.Tests;

public class SiteVerificationFileMapHandlerTests
{
    [Fact]
    public async Task Handle_WhenFileExists_ReturnsTextWithCacheHeaders()
    {
        const string fileName = "google123.html";
        const string content = "verification-token";
        var file = CreatePublicFile(fileName, content);
        var cache = CreateCache(fileName, file);
        var httpContext = CreateHttpContext();

        var result = await SiteVerificationFileMapHandler.Handle(
            fileName,
            httpContext,
            cache.Object,
            Mock.Of<IQueryMediator>());

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", httpContext.Response.ContentType);
        Assert.Equal("public, max-age=300", httpContext.Response.Headers.CacheControl.ToString());
        Assert.Equal(file.EntityTag, httpContext.Response.Headers.ETag.ToString());

        var body = await ReadBodyAsync(httpContext);
        Assert.Equal(content, body);
    }

    [Fact]
    public async Task Handle_WhenRequestIsHead_ReturnsHeadersWithoutBody()
    {
        const string fileName = "google123.txt";
        var file = CreatePublicFile(fileName, "verification-token");
        var cache = CreateCache(fileName, file);
        var httpContext = CreateHttpContext();
        httpContext.Request.Method = HttpMethods.Head;

        var result = await SiteVerificationFileMapHandler.Handle(
            fileName,
            httpContext,
            cache.Object,
            Mock.Of<IQueryMediator>());

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", httpContext.Response.ContentType);
        Assert.Equal(18, httpContext.Response.ContentLength);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task Handle_WhenIfNoneMatchMatches_ReturnsNotModified()
    {
        const string fileName = "google123.txt";
        var file = CreatePublicFile(fileName, "verification-token");
        var cache = CreateCache(fileName, file);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.IfNoneMatch = file.EntityTag;

        var result = await SiteVerificationFileMapHandler.Handle(
            fileName,
            httpContext,
            cache.Object,
            Mock.Of<IQueryMediator>());

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status304NotModified, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task Handle_WhenIfModifiedSinceMatchesOnlyTruncatedSecond_ReturnsOk()
    {
        const string fileName = "google123.txt";
        const string content = "verification-token";
        var preciseLastModified = new DateTime(2026, 8, 9, 1, 2, 3, 500, DateTimeKind.Utc);
        var file = CreatePublicFile(fileName, content, preciseLastModified);
        var cache = CreateCache(fileName, file);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.IfModifiedSince = "Sun, 09 Aug 2026 01:02:03 GMT";

        var result = await SiteVerificationFileMapHandler.Handle(
            fileName,
            httpContext,
            cache.Object,
            Mock.Of<IQueryMediator>());

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("Sun, 09 Aug 2026 01:02:03 GMT", httpContext.Response.Headers.LastModified.ToString());

        var body = await ReadBodyAsync(httpContext);
        Assert.Equal(content, body);
    }

    [Fact]
    public async Task Handle_WhenFileNameIsInvalid_ReturnsNotFoundWithoutQuery()
    {
        var cache = new Mock<ICacheAside>();
        var queryMediator = new Mock<IQueryMediator>();
        var httpContext = CreateHttpContext();

        var result = await SiteVerificationFileMapHandler.Handle(
            "../google.txt",
            httpContext,
            cache.Object,
            queryMediator.Object);

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        cache.Verify(
            x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<PublicSiteVerificationFile>>>()),
            Times.Never);
        queryMediator.Verify(
            x => x.QueryAsync(
                It.IsAny<GetPublicSiteVerificationFileQuery>(),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFileDoesNotExist_ReturnsNotFound()
    {
        const string fileName = "missing.txt";
        var cache = CreateCache(fileName, null);
        var httpContext = CreateHttpContext();

        var result = await SiteVerificationFileMapHandler.Handle(
            fileName,
            httpContext,
            cache.Object,
            Mock.Of<IQueryMediator>());

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    private static Mock<ICacheAside> CreateCache(string fileName, PublicSiteVerificationFile? file)
    {
        var cache = new Mock<ICacheAside>();
        cache
            .Setup(x => x.GetOrCreateAsync(
                BlogCachePartition.General.ToString(),
                SiteVerificationFileMapHandler.GetCacheKey(fileName),
                It.IsAny<Func<Task<PublicSiteVerificationFile>>>()))
            .ReturnsAsync(file);

        return cache;
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        return httpContext;
    }

    private static PublicSiteVerificationFile CreatePublicFile(
        string fileName,
        string content,
        DateTime? lastModifiedUtc = null)
    {
        var lastModified = lastModifiedUtc ?? new DateTime(2026, 8, 9, 1, 2, 3, DateTimeKind.Utc);
        var contentBytes = Encoding.UTF8.GetByteCount(content);
        return new PublicSiteVerificationFile(
            fileName,
            content,
            SiteVerificationFileConstants.GetContentType(fileName),
            lastModified,
            $"\"{lastModified.Ticks:x}-{contentBytes:x}\"");
    }

    private static async Task<string> ReadBodyAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
