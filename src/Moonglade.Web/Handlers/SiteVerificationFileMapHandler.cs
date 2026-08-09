using LiteBus.Queries.Abstractions;
using Microsoft.Net.Http.Headers;
using Moonglade.Features.SiteVerification;

namespace Moonglade.Web.Handlers;

public class SiteVerificationFileMapHandler
{
    private const string CacheKeyPrefix = "site-verification-file:";

    public static Delegate Handler => Handle;

    public static async Task<IResult> Handle(
        string filename,
        HttpContext httpContext,
        ICacheAside cache,
        IQueryMediator queryMediator)
    {
        var validation = SiteVerificationFileConstants.ValidateFileName(filename);
        if (!validation.Succeeded)
        {
            return Results.NotFound();
        }

        var cacheKey = GetCacheKey(filename);
        var file = await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), cacheKey, async () =>
            await queryMediator.QueryAsync(
                new GetPublicSiteVerificationFileQuery(filename),
                cancellationToken: httpContext.RequestAborted));

        if (file == null)
        {
            return Results.NotFound();
        }

        var entityTag = EntityTagHeaderValue.Parse(file.EntityTag);
        var preciseLastModified = new DateTimeOffset(DateTime.SpecifyKind(file.LastModifiedTimeUtc, DateTimeKind.Utc));
        var responseLastModified = new DateTimeOffset(TruncateToSeconds(preciseLastModified.UtcDateTime), TimeSpan.Zero);

        var typedHeaders = httpContext.Response.GetTypedHeaders();
        typedHeaders.CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(5)
        };
        typedHeaders.ETag = entityTag;
        typedHeaders.LastModified = responseLastModified;

        if (IsNotModified(httpContext, entityTag, preciseLastModified))
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        if (HttpMethods.IsHead(httpContext.Request.Method))
        {
            httpContext.Response.ContentType = file.ContentType;
            httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(file.Content);
            return Results.Empty;
        }

        return Results.Text(file.Content, file.ContentType, Encoding.UTF8);
    }

    public static string GetCacheKey(string fileName) =>
        $"{CacheKeyPrefix}{SiteVerificationFileConstants.NormalizeFileName(fileName)}";

    private static bool IsNotModified(
        HttpContext httpContext,
        EntityTagHeaderValue entityTag,
        DateTimeOffset lastModified)
    {
        var requestHeaders = httpContext.Request.GetTypedHeaders();

        if (requestHeaders.IfNoneMatch is { Count: > 0 })
        {
            return requestHeaders.IfNoneMatch.Any(tag =>
                string.Equals(tag.ToString(), "*", StringComparison.Ordinal) ||
                string.Equals(tag.ToString(), entityTag.ToString(), StringComparison.Ordinal));
        }

        if (requestHeaders.IfModifiedSince.HasValue)
        {
            return lastModified <= requestHeaders.IfModifiedSince.Value;
        }

        return false;
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
}
