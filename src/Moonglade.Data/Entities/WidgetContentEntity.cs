using System.Text.Json.Serialization;

namespace Moonglade.Data.Entities;

public class WidgetContentEntity
{
    public Guid Id { get; set; }
    public Guid WidgetId { get; set; }
    public string Title { get; set; }
    public string ContentType { get; set; }
    public string ContentCode { get; set; }

    [JsonIgnore]
    public WidgetEntity Widget { get; set; }
}