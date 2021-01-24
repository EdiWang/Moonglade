using System.Text.Json.Serialization;

namespace Moonglade.Web.Models
{
    public class ManifestIcon
    {
        public string Src => "/" + string.Format(SrcTemplate ?? string.Empty, Sizes);
        public string Sizes => $"{Pixel}x{Pixel}";
        public string Type { get; set; }
        public string Density { get; set; }

        [JsonIgnore]
        public string SrcTemplate { get; set; }

        [JsonIgnore]
        public int Pixel { get; set; }
    }
}