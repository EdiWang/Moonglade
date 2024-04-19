using System.Text.Json.Serialization;

namespace Moonglade.Data.Entities;

public class PostCategoryEntity
{
    public Guid PostId { get; set; }
    public Guid CategoryId { get; set; }

    [JsonIgnore]
    public virtual CategoryEntity Category { get; set; }

    [JsonIgnore]
    public virtual PostEntity Post { get; set; }
}