using System.Text.Json.Serialization;

namespace Moonglade.Web.Handlers;

public class WebManifestMapHandler
{
    public static Delegate Handler => Handle;

    public static IResult Handle(HttpContext httpContext, IBlogConfig blogConfig)
    {
        var model = new ManifestModel
        {
            ShortName = blogConfig.GeneralSettings.SiteTitle,
            Name = blogConfig.GeneralSettings.SiteTitle,
            Description = blogConfig.GeneralSettings.Description,
            StartUrl = "/",
            Icons =
            [
                new() { Pixel = 144, SrcTemplate = "android-icon-{0}.png" },
                new() { Pixel = 192, SrcTemplate = "android-icon-{0}.png" }
            ],
            BackgroundColor = "#333333",
            ThemeColor = "#333333",
            Display = "standalone",
            Orientation = "portrait"
        };

        httpContext.Response.Headers.CacheControl = "public, max-age=3600";

        return Results.Json(model, contentType: "application/manifest+json");
    }
}

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
