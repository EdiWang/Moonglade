using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets;

public record CreateWidgetCommand(EditWidgetRequest Payload) : ICommand<Guid>;

public class CreateWidgetCommandHandler(
    BlogDbContext db,
    ILogger<CreateWidgetCommandHandler> logger,
    WidgetCollectionGate gate) : ICommandHandler<CreateWidgetCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateWidgetCommand request, CancellationToken ct)
    {
        Validator.ValidateObject(request.Payload, new ValidationContext(request.Payload), true);
        var id = Guid.NewGuid();
        var createdTime = DateTime.UtcNow;
        await gate.ExecuteAsync(async token =>
        {
            if (await db.Widget.FindAsync([id], token) is not null) return id;
            var widget = new WidgetEntity
            {
                Id = id,
                Title = request.Payload.Title.Trim(),
                WidgetType = request.Payload.WidgetType,
                ContentType = WidgetContentType.JSON, // Hardcoded to JSON for now
                ContentCode = request.Payload.ContentCode?.Trim(),
                Region = request.Payload.Region,
                SortOrder = request.Payload.SortOrder,
                IsEnabled = request.Payload.IsEnabled,
                CreatedTimeUtc = createdTime
            };

            db.Widget.Add(widget);
            await db.SaveChangesAsync(token);
            return id;
        }, ct);

        logger.LogInformation("Widget created: {WidgetId}", id);

        return id;
    }
}
