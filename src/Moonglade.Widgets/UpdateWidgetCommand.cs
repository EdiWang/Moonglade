using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public record UpdateWidgetCommand(Guid Id, EditWidgetRequest Payload) : ICommand<OperationCode>;

public class UpdateWidgetCommandHandler(
    MoongladeRepository<WidgetEntity> widgetRepo,
    ILogger<UpdateWidgetCommandHandler> logger) : ICommandHandler<UpdateWidgetCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateWidgetCommand request, CancellationToken ct)
    {
        var widget = await widgetRepo.GetByIdAsync(request.Id, ct);
        if (widget is null) return OperationCode.ObjectNotFound;

        widget.Title = request.Payload.Title.Trim();
        // WidgetType is intentionally not updated - it's immutable after creation
        widget.DisplayOrder = request.Payload.DisplayOrder;
        widget.IsEnabled = request.Payload.IsEnabled;

        await widgetRepo.UpdateAsync(widget, ct);

        logger.LogInformation("Widget updated: {WidgetId}", widget.Id);
        return OperationCode.Done;
    }
}