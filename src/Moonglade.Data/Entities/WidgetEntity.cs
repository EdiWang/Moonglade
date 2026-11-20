namespace Moonglade.Data.Entities;

public class WidgetEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public WidgetType WidgetType { get; set; }
    public WidgetContentType ContentType { get; set; }
    public string ContentCode { get; set; }
    public WidgetRegion Region { get; set; } = WidgetRegion.AfterCategories;
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedTimeUtc { get; set; }
}
