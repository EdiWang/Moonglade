using System.Text.Json.Serialization;

namespace Moonglade.Data.Entities;

public class CategoryEntity
{
    public CategoryEntity()
    {
        PostCategory = new HashSet<PostCategoryEntity>();
    }

    public Guid Id { get; set; }
    public string Slug { get; set; }
    public string DisplayName { get; set; }
    public string Note { get; set; }

    [JsonIgnore]
    public ICollection<PostCategoryEntity> PostCategory { get; set; }
}