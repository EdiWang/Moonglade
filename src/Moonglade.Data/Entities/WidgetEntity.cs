using System.Text.Json.Serialization;

namespace Moonglade.Data.Entities;

public class WidgetEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public WidgetType WidgetType { get; set; }
    public WidgetContentType ContentType { get; set; }
    public string ContentCode { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedTimeUtc { get; set; }
}