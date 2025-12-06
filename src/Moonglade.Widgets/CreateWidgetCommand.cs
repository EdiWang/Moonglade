using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public record CreateWidgetCommand(EditWidgetRequest Payload) : ICommand<Guid>;

public class CreateWidgetCommandHandler(
    MoongladeRepository<WidgetEntity> widgetRepo,
    ILogger<CreateWidgetCommandHandler> logger) : ICommandHandler<CreateWidgetCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateWidgetCommand request, CancellationToken ct)
    {
        var widget = new WidgetEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Payload.Title.Trim(),
            WidgetType = request.Payload.WidgetType,
            ContentType = WidgetContentType.JSON, // Hardcoded to JSON for now
            ContentCode = request.Payload.ContentCode.Trim(),
            DisplayOrder = request.Payload.DisplayOrder,
            IsEnabled = request.Payload.IsEnabled,
            CreatedTimeUtc = DateTime.UtcNow
        };

        await widgetRepo.AddAsync(widget, ct);

        logger.LogInformation("Widget created: {WidgetId}", widget.Id);

        return widget.Id;
    }
}