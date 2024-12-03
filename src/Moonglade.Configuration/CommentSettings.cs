using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class CommentSettings : IBlogSettings
{
    [JsonIgnore]
    public static CommentSettings DefaultValue => new()
    {
    };
}