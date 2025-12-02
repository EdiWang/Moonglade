using System.Text.Json.Serialization;

namespace Moonglade.Data.Entities;

public class WidgetEntity
{
    public WidgetEntity()
    {
        LinkItems = new HashSet<WidgetLinkItemEntity>();
    }

    public Guid Id { get; set; }
    public string Title { get; set; }
    public WidgetType WidgetType { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedTimeUtc { get; set; }

    [JsonIgnore]
    public ICollection<WidgetLinkItemEntity> LinkItems { get; set; }
}