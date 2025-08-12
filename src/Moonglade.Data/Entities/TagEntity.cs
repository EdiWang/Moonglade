using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Data.Entities;

public class TagEntity
{
    public TagEntity()
    {
        Posts = new HashSet<PostEntity>();
    }

    public int Id { get; set; }

    [MaxLength(32)]
    public string DisplayName { get; set; }

    [MaxLength(32)]
    public string NormalizedName { get; set; }

    [JsonIgnore]
    public ICollection<PostEntity> Posts { get; set; }
}