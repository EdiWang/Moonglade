using System.Text.Json.Serialization;

namespace Moonglade.Data.Specifications;

[JsonConverter(typeof(JsonStringEnumConverter<PostStatus>))]
public enum PostStatus
{
    Default = 0,
    Draft = 1,
    Scheduled = 2,
    Published = 3,
    Deleted = 4
}