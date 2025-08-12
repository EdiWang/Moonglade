using System.Text.Json.Serialization;

namespace Moonglade.Data.Entities;

public class PostCategoryEntity
{
    public Guid PostId { get; set; }
    public Guid CategoryId { get; set; }

    [JsonIgnore]
    public CategoryEntity Category { get; set; }

    [JsonIgnore]
    public PostEntity Post { get; set; }
}