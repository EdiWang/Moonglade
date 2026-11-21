using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public sealed record WidgetCollection(IReadOnlyList<WidgetSnapshot> Widgets);

public sealed record WidgetSnapshot(Guid Id, string Title, WidgetType WidgetType,
    WidgetContentType ContentType, string ContentCode, WidgetRegion Region,
    int SortOrder, bool IsEnabled, DateTime CreatedTimeUtc)
{
    public static WidgetSnapshot From(WidgetEntity widget) => new(widget.Id, widget.Title,
        widget.WidgetType, widget.ContentType, widget.ContentCode, widget.Region,
        widget.SortOrder, widget.IsEnabled, widget.CreatedTimeUtc);
}

public enum WidgetChangeKind { Created, Updated, Deleted }

public sealed record WidgetChange(WidgetChangeKind Kind, Guid Id, string Title, WidgetType WidgetType);

public sealed record WidgetCollectionSaveResult(WidgetCollection Collection, IReadOnlyList<WidgetChange> Changes);
