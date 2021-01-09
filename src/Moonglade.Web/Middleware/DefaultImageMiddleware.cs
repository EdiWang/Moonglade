using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration.Abstraction;

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
                blogConfig.ContentSettings.UseFriendlyNotFoundImage)
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

            var fs = File.OpenRead(path);
            var bytes = new byte[fs.Length];

            await fs.ReadAsync(bytes.AsMemory(0, bytes.Length));

            //this header is use for browser cache, format like: "Mon, 15 May 2017 07:03:37 GMT".
            //context.Response.Headers.Append("Last-Modified", $"{File.GetLastWriteTimeUtc(path).ToString("ddd, dd MMM yyyy HH:mm:ss")} GMT");
            await context.Response.Body.WriteAsync(bytes.AsMemory(0, bytes.Length));
        }
    }

    public static class DefaultImageMiddlewareExtensions
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