using Microsoft.AspNetCore.Http.Extensions;
using Moonglade.Configuration;

namespace Moonglade.Web.Middleware
{
    // credits: conan5566
    public class DefaultImageMiddleware
    {
        private readonly RequestDelegate _next;

        public static DefaultImageMiddlewareOptions Options { get; set; } = new();

        public DefaultImageMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IBlogConfig blogConfig)
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status404NotFound &&
                context.Request.Path.StartsWithSegments("/image") &&
                blogConfig.ImageSettings.UseFriendlyNotFoundImage)
            {
                var ext = Path.GetExtension(context.Request.Path);
                var contentType = context.Request.Headers["accept"].ToString().ToLower();

                if (!string.IsNullOrWhiteSpace(ext) && Options.AllowedExtensions.Contains(ext) || contentType.StartsWith("image"))
                {
                    await SetDefaultImage(context);
                }
            }
        }

        private static async Task SetDefaultImage(HttpContext context)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), Options.DefaultImagePath);
            if (!File.Exists(path)) return;

            // Reference: https://source.dot.net/#Microsoft.AspNetCore.Mvc.Core/Infrastructure/FileResultExecutorBase.cs,408
            var fs = File.OpenRead(path);
            await using (fs)
            {
                try
                {
                    await StreamCopyOperation.CopyToAsync(
                        fs, context.Response.Body, null, 64 * 1024, context.RequestAborted);

                    //this header is use for browser cache, format like: "Mon, 15 May 2017 07:03:37 GMT".
                    context.Response.Headers.Append("Last-Modified", $"{File.GetLastWriteTimeUtc(path):ddd, dd MMM yyyy HH:mm:ss} GMT");
                }
                catch (OperationCanceledException)
                {
                    // Don't throw this exception, it's most likely caused by the client disconnecting.
                    // However, if it was cancelled for any other reason we need to prevent empty responses.
                    context.Abort();
                }
            }
        }
    }

    public static partial class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDefaultImage(this IApplicationBuilder app, Action<DefaultImageMiddlewareOptions> options)
        {
            options(DefaultImageMiddleware.Options);
            return app.UseMiddleware<DefaultImageMiddleware>();
        }
    }

    public class DefaultImageMiddlewareOptions
    {
        public string DefaultImagePath { get; set; }
        public IEnumerable<string> AllowedExtensions { get; set; }

        public DefaultImageMiddlewareOptions()
        {
            AllowedExtensions = Array.Empty<string>();
        }
    }
}