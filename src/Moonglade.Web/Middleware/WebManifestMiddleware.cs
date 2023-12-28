using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace Moonglade.Web.Middleware;

public class WebManifestMiddleware(RequestDelegate next)
{
    public static WebManifestMiddlewareOptions Options { get; set; } = new();

    public async Task Invoke(
        HttpContext context, IBlogConfig blogConfig, IOptions<List<ManifestIcon>> manifestIcons)
    {
        var model = new ManifestModel
        {
            ShortName = blogConfig.GeneralSettings.SiteTitle,
            Name = blogConfig.GeneralSettings.SiteTitle,
            Description = blogConfig.GeneralSettings.Description,
            StartUrl = "/",
            Icons = manifestIcons?.Value,
            BackgroundColor = Options.ThemeColor,
            ThemeColor = Options.ThemeColor,
            Display = "standalone",
            Orientation = "portrait"
        };

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/manifest+json";
        context.Response.Headers.TryAdd("cache-control", "public,max-age=3600");

        // Do not use `WriteAsJsonAsync` because it will override ContentType header
        await context.Response.WriteAsync(model.ToJson(true), context.RequestAborted);
    }
}

// Credits: https://github.com/Anduin2017/Blog
public class ManifestModel
{
    [JsonPropertyName("short_name")]
    public string ShortName { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    [JsonPropertyName("start_url")]
    public string StartUrl { get; set; }

    public IEnumerable<ManifestIcon> Icons { get; set; }

    [JsonPropertyName("background_color")]
    public string BackgroundColor { get; set; }

    [JsonPropertyName("theme_color")]
    public string ThemeColor { get; set; }

    public string Display { get; set; }
    public string Orientation { get; set; }
}

public class ManifestIcon
{
    public string Src => "/" + string.Format(SrcTemplate ?? string.Empty, Sizes);
    public string Sizes => $"{Pixel}x{Pixel}";
    public string Type => "image/png";

    [JsonIgnore]
    public string SrcTemplate { get; set; }

    [JsonIgnore]
    public int Pixel { get; set; }
}

public static partial class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseManifest(this IApplicationBuilder app, Action<WebManifestMiddlewareOptions> options)
    {
        options(WebManifestMiddleware.Options);

        app.UseWhen(
            ctx => ctx.Request.Path == "/manifest.webmanifest",
            appBuilder => appBuilder.UseMiddleware<WebManifestMiddleware>()
        );

        return app;
    }
}

public class WebManifestMiddlewareOptions
{
    public string ThemeColor { get; set; } = "#333333";
}