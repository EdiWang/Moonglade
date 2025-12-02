using System.Text.Json.Serialization;

namespace Moonglade.Data.Entities;

public class WidgetLinkItemEntity
{
    public Guid Id { get; set; }
    public Guid WidgetId { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public string IconName { get; set; }
    public bool OpenInNewWindow { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsEnabled { get; set; }

    [JsonIgnore]
    public WidgetEntity Widget { get; set; }
}