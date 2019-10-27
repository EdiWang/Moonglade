using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Moonglade.Web.Models
{
    // Credits: https://github.com/Anduin2017/Blog
    public class ManifestModel
    {
        [JsonPropertyName("short_name")]
        public string ShortName { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        [JsonPropertyName("start_url")]
        public string StartUrl { get; set; }

        public List<ManifestIcon> Icons { get; set; }

        [JsonPropertyName("background_color")]
        public string BackgroundColor { get; set; }

        [JsonPropertyName("theme_color")]
        public string ThemeColor { get; set; }

        public string Display { get; set; }
        public string Orientation { get; set; }
    }

    public class ManifestIcon
    {
        public string Src { get; set; }
        public string Sizes { get; set; }
        public string Type { get; set; }
        public string Density { get; set; }

        public ManifestIcon(string srcTemplate, int sizePixel, string density)
        {
            Sizes = $"{sizePixel}x{sizePixel}";
            Type = "image/png";
            Density = density;
            Src = string.Format(srcTemplate, Sizes);
        }
    }
}
