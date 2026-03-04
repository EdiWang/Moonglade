using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Features.Page;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Moonglade.Web.Middleware;

public class StyleSheetMiddleware(
    RequestDelegate next,
    ILogger<StyleSheetMiddleware> logger,
    IOptions<StyleSheetMiddlewareOptions> options)
{
    private readonly StyleSheetMiddlewareOptions _options = options.Value;

    public async Task Invoke(HttpContext context, IBlogConfig blogConfig, IQueryMediator queryMediator)
    {
        try
        {
            var requestPath = context.Request.Path.ToString();

            // Early exit for non-CSS requests
            if (!IsCssRequest(requestPath))
            {
                await next(context);
                return;
            }

            // Sanitize path to prevent path traversal attacks
            if (ContainsSuspiciousCharacters(requestPath))
            {
                logger.LogWarning("Suspicious path detected: {RequestPath}", requestPath);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var normalizedPath = requestPath.ToLowerInvariant();

            if (string.Equals(normalizedPath, _options.DefaultPath.Value?.ToLowerInvariant(), StringComparison.Ordinal))
            {
                await HandleDefaultPath(context, blogConfig);
            }
            else if (string.Equals(normalizedPath, "/content.css", StringComparison.Ordinal))
            {
                await HandleContentCss(context, queryMediator);
            }
            else
            {
                await next(context);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing CSS request for path: {RequestPath}", context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }
    }

    private static bool IsCssRequest(string requestPath)
    {
        return requestPath.EndsWith(".css", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsSuspiciousCharacters(string path)
    {
        // Check for path traversal attempts and other suspicious patterns
        return path.Contains("..", StringComparison.Ordinal) ||
               path.Contains('~', StringComparison.Ordinal) ||
               path.Contains('\0') ||
               path.Contains("%00") ||
               path.Contains("%5C") || // Encoded backslash
               path.Contains('\\', StringComparison.Ordinal); // Only check for backslashes, forward slashes are normal
    }

    private async Task HandleDefaultPath(HttpContext context, IBlogConfig blogConfig)
    {
        if (!blogConfig.AppearanceSettings.EnableCustomCss)
        {
            logger.LogDebug("Custom CSS is disabled");
            await WriteNotFoundResponse(context);
            return;
        }

        var cssCode = blogConfig.AppearanceSettings.CssCode;

        if (string.IsNullOrWhiteSpace(cssCode))
        {
            logger.LogDebug("Custom CSS code is empty");
            await WriteNotFoundResponse(context);
            return;
        }

        await WriteStyleSheet(context, cssCode);
    }

    private async Task HandleContentCss(HttpContext context, IQueryMediator queryMediator)
    {
        var queryString = context.Request.QueryString.Value;

        if (string.IsNullOrWhiteSpace(queryString))
        {
            await WriteNotFoundResponse(context);
            return;
        }

        var qs = HttpUtility.ParseQueryString(queryString);
        var id = qs["id"];

        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var guid))
        {
            logger.LogDebug("Invalid or missing stylesheet ID: {Id}", id);
            await WriteNotFoundResponse(context);
            return;
        }

        try
        {
            var css = await queryMediator.QueryAsync(new GetStyleSheetQuery(guid), context.RequestAborted);

            if (css?.CssContent == null)
            {
                logger.LogDebug("Stylesheet not found for ID: {Id}", guid);
                await WriteNotFoundResponse(context);
                return;
            }

            await WriteStyleSheet(context, css.CssContent, css.LastModifiedTimeUtc);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Request was cancelled for stylesheet ID: {Id}", guid);
            // Don't write response if request was cancelled
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving stylesheet for ID: {Id}", guid);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }
    }

    private static async Task WriteNotFoundResponse(HttpContext context)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Stylesheet not found", context.RequestAborted);
        }
    }

    private async Task WriteStyleSheet(HttpContext context, string cssCode, DateTime? lastModified = null)
    {
        if (string.IsNullOrWhiteSpace(cssCode))
        {
            await WriteNotFoundResponse(context);
            return;
        }

        var cssLength = Encoding.UTF8.GetByteCount(cssCode);

        if (cssLength > _options.MaxContentLength)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("CSS content too large", context.RequestAborted);
            return;
        }

        var etag = GenerateETag(cssCode);

        SetCachingHeaders(context, etag, lastModified);

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

    private void SetCachingHeaders(HttpContext context, string etag, DateTime? lastModified)
    {
        var response = context.Response;

        response.Headers.ETag = etag;

        if (lastModified.HasValue)
        {
            response.Headers.LastModified = lastModified.Value.ToString("R");
        }

        response.Headers.CacheControl = $"public, max-age={_options.CacheMaxAge}";
        response.Headers.Expires = DateTime.UtcNow.AddSeconds(_options.CacheMaxAge).ToString("R");
    }

    private static string GenerateETag(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return $"\"{Convert.ToHexString(hash)[..16]}\"";
    }

    private static bool IsNotModified(HttpContext context, string etag, DateTime? lastModified)
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
    public static IApplicationBuilder UseCustomCss(this IApplicationBuilder app, Action<StyleSheetMiddlewareOptions> configure = null)
    {
        var opt = new StyleSheetMiddlewareOptions();
        configure?.Invoke(opt);
        return app.UseMiddleware<StyleSheetMiddleware>(Options.Create(opt));
    }
}

public class StyleSheetMiddlewareOptions
{
    public int MaxContentLength { get; set; } = 65536;
    public PathString DefaultPath { get; set; } = "/custom.css";
    public int CacheMaxAge { get; set; } = 3600; // 1 hour in seconds
}