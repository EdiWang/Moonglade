using System.Text.Json;

namespace Moonglade.Configuration
{
    public static class JsonExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static T FromJson<T>(this string json) => JsonSerializer.Deserialize<T>(json, JsonOptions);

        public static string ToJson<T>(this T obj) => JsonSerializer.Serialize(obj, JsonOptions);
    }
}
