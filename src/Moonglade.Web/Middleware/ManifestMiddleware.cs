using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Models;

namespace Moonglade.Web.Middleware
{
    public class ManifestMiddleware
    {
        private readonly RequestDelegate _next;

        public static ManifestMiddlewareOptions Options { get; set; } = new();

        public ManifestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(
            HttpContext context, IBlogConfig blogConfig, IOptions<List<ManifestIcon>> manifestIcons)
        {
            if (context.Request.Path == "/manifest.json")
            {
                var model = new ManifestModel
                {
                    ShortName = blogConfig.GeneralSettings.SiteTitle,
                    Name = blogConfig.GeneralSettings.SiteTitle,
                    Description = blogConfig.GeneralSettings.SiteTitle,
                    StartUrl = "/",
                    Icons = manifestIcons?.Value,
                    BackgroundColor = Options.ThemeColor,
                    ThemeColor = Options.ThemeColor,
                    Display = "standalone",
                    Orientation = "portrait"
                };

                var json = JsonSerializer.Serialize(model);

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                context.Response.Headers.TryAdd("cache-control", "public,max-age=3600");

                await context.Response.WriteAsync(json);
            }
            else
            {
                await _next(context);
            }
        }
    }

    public static class ManifestMiddlewareOptionsExtensions
    {
        public static IApplicationBuilder UseManifest(this IApplicationBuilder app, Action<ManifestMiddlewareOptions> options)
        {
            options(ManifestMiddleware.Options);
            return app.UseMiddleware<ManifestMiddleware>();
        }
    }

    public class ManifestMiddlewareOptions
    {
        public string ThemeColor { get; set; }

        public ManifestMiddlewareOptions()
        {
            ThemeColor = "#333333";
        }
    }
}
