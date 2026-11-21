using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets;

public record SaveWidgetCollectionCommand(WidgetCollectionRequest Payload) : ICommand<WidgetCollectionSaveResult>;

public class SaveWidgetCollectionCommandHandler(BlogDbContext db, WidgetCollectionGate gate)
    : ICommandHandler<SaveWidgetCollectionCommand, WidgetCollectionSaveResult>
{
    public async Task<WidgetCollectionSaveResult> HandleAsync(SaveWidgetCollectionCommand request, CancellationToken ct)
    {
        Validator.ValidateObject(request.Payload, new ValidationContext(request.Payload), true);
        var draft = request.Payload.Widgets!
            .Select((widget, index) => (Widget: widget!, Index: index))
            .OrderBy(item => item.Widget.Region).ThenBy(item => item.Widget.SortOrder).ThenBy(item => item.Index)
            .ToArray();
        WidgetCollectionSaveResult previousAttempt = null;

        return await gate.ExecuteAsync(async token =>
        {
            var current = await db.Widget.ToDictionaryAsync(widget => widget.Id, token);
            // A lost commit acknowledgment must preserve the original saved result and audit snapshots.
            if (previousAttempt is not null && current.Count == previousAttempt.Collection.Widgets.Count
                && previousAttempt.Collection.Widgets.All(widget => current.TryGetValue(widget.Id, out var row)
                    && WidgetSnapshot.From(row) == widget))
                return previousAttempt;
            var submittedIds = draft.Select(item => item.Widget.Id).ToHashSet();
            List<WidgetChange> changes = [];
            foreach (var removed in current.Values.Where(widget => !submittedIds.Contains(widget.Id)))
            {
                db.Widget.Remove(removed);
                changes.Add(new WidgetChange(WidgetChangeKind.Deleted, removed.Id, removed.Title, removed.WidgetType));
            }

            var regionOrder = new Dictionary<WidgetRegion, int>();
            foreach (var (widget, _) in draft)
            {
                var exists = current.TryGetValue(widget.Id, out var entity);
                if (!exists)
                {
                    entity = new WidgetEntity { Id = widget.Id, CreatedTimeUtc = DateTime.UtcNow };
                    db.Widget.Add(entity);
                }
                var before = WidgetSnapshot.From(entity);
                entity.Title = widget.Title!.Trim();
                entity.WidgetType = widget.WidgetType;
                entity.ContentType = WidgetContentType.JSON;
                entity.ContentCode = widget.ContentCode!.Trim();
                entity.Region = widget.Region;
                entity.SortOrder = regionOrder.GetValueOrDefault(widget.Region);
                regionOrder[widget.Region] = entity.SortOrder + 1;
                entity.IsEnabled = widget.IsEnabled;
                var after = WidgetSnapshot.From(entity);
                if (!exists || before != after)
                    changes.Add(new WidgetChange(exists ? WidgetChangeKind.Updated : WidgetChangeKind.Created,
                        entity.Id, entity.Title, entity.WidgetType));
            }
            await db.SaveChangesAsync(token);
            var persisted = await db.Widget.AsNoTracking().ToListAsync(token);
            var saved = persisted.OrderBy(widget => widget.Region).ThenBy(widget => widget.SortOrder)
                .Select(WidgetSnapshot.From).ToArray();
            previousAttempt = new WidgetCollectionSaveResult(new WidgetCollection(saved), changes);
            return previousAttempt;
        }, ct);
    }
}
