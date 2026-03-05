using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Features.Page;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Moonglade.Web.Middleware;

public class StyleSheetHandler
{
    public static async Task HandleCustomCssAsync(
        HttpContext context,
        IBlogConfig blogConfig,
        StyleSheetOptions options,
        ILogger logger)
    {
        if (!blogConfig.AppearanceSettings.EnableCustomCss)
        {
            logger.LogDebug("Custom CSS is disabled");
            await WriteNotFoundAsync(context);
            return;
        }

        var cssCode = blogConfig.AppearanceSettings.CssCode;

        if (string.IsNullOrWhiteSpace(cssCode))
        {
            logger.LogDebug("Custom CSS code is empty");
            await WriteNotFoundAsync(context);
            return;
        }

        await WriteStyleSheetAsync(context, cssCode, null, options);
    }

    public static async Task HandleContentCssAsync(
        HttpContext context,
        IQueryMediator queryMediator,
        StyleSheetOptions options,
        ILogger logger)
    {
        if (!context.Request.Query.TryGetValue("id", out var idValue) ||
            !Guid.TryParse(idValue, out var guid))
        {
            logger.LogDebug("Invalid or missing stylesheet ID");
            await WriteNotFoundAsync(context);
            return;
        }

        try
        {
            var css = await queryMediator.QueryAsync(new GetStyleSheetQuery(guid), context.RequestAborted);

            if (css?.CssContent == null)
            {
                logger.LogDebug("Stylesheet not found for ID: {Id}", guid);
                await WriteNotFoundAsync(context);
                return;
            }

            await WriteStyleSheetAsync(context, css.CssContent, css.LastModifiedTimeUtc, options);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Request was cancelled for stylesheet ID: {Id}", guid);
        }
    }

    public static async Task WriteStyleSheetAsync(
        HttpContext context,
        string cssCode,
        DateTime? lastModified,
        StyleSheetOptions options)
    {
        var cssLength = Encoding.UTF8.GetByteCount(cssCode);

        if (cssLength > options.MaxContentLength)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("CSS content too large", context.RequestAborted);
            return;
        }

        var etag = GenerateETag(cssCode);

        SetCachingHeaders(context, etag, lastModified, options);

        if (IsNotModified(context, etag, lastModified))
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/css; charset=utf-8";
        context.Response.ContentLength = cssLength;
        await context.Response.WriteAsync(cssCode, context.RequestAborted);
    }

    private static async Task WriteNotFoundAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Stylesheet not found", context.RequestAborted);
    }

    private static void SetCachingHeaders(HttpContext context, string etag, DateTime? lastModified, StyleSheetOptions options)
    {
        context.Response.Headers.ETag = etag;

        if (lastModified.HasValue)
        {
            context.Response.Headers.LastModified = lastModified.Value.ToString("R");
        }

        context.Response.Headers.CacheControl = $"public, max-age={options.CacheMaxAge}";
        context.Response.Headers.Expires = DateTime.UtcNow.AddSeconds(options.CacheMaxAge).ToString("R");
    }

    public static string GenerateETag(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return $"\"{Convert.ToHexString(hash)[..16]}\"";
    }

    public static bool IsNotModified(HttpContext context, string etag, DateTime? lastModified)
    {
        var request = context.Request;

        if (request.Headers.IfNoneMatch.Count > 0)
        {
            return request.Headers.IfNoneMatch.Any(value =>
                string.Equals(value, etag, StringComparison.Ordinal) ||
                string.Equals(value, "*", StringComparison.Ordinal));
        }

        if (lastModified.HasValue &&
            request.Headers.IfModifiedSince.Count > 0 &&
            DateTime.TryParseExact(
                request.Headers.IfModifiedSince,
                "R",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal,
                out var ifModifiedSince))
        {
            return lastModified.Value <= ifModifiedSince;
        }

        return false;
    }
}

public static partial class ApplicationBuilderExtensions
{
    public static IEndpointRouteBuilder MapStyleSheets(
        this IEndpointRouteBuilder endpoints,
        Action<StyleSheetOptions> configure = null)
    {
        var options = new StyleSheetOptions();
        configure?.Invoke(options);

        endpoints.MapGet("/custom.css",
            (IBlogConfig blogConfig, HttpContext ctx, ILogger<StyleSheetHandler> logger) =>
                StyleSheetHandler.HandleCustomCssAsync(ctx, blogConfig, options, logger));

        endpoints.MapGet("/content.css",
            (IQueryMediator queryMediator, HttpContext ctx, ILogger<StyleSheetHandler> logger) =>
                StyleSheetHandler.HandleContentCssAsync(ctx, queryMediator, options, logger));

        return endpoints;
    }
}

public class StyleSheetOptions
{
    public int MaxContentLength { get; set; } = 65536;
    public int CacheMaxAge { get; set; } = 3600;
}
