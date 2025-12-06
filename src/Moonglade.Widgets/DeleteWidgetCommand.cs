using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public record DeleteWidgetCommand(Guid Id) : ICommand<OperationCode>;

public class DeleteWidgetCommandHandler(
    MoongladeRepository<WidgetEntity> widgetRepo,
    ILogger<DeleteWidgetCommandHandler> logger) : ICommandHandler<DeleteWidgetCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteWidgetCommand request, CancellationToken ct)
    {
        var widget = await widgetRepo.GetByIdAsync(request.Id, ct);
        if (widget is null) return OperationCode.ObjectNotFound;

        await widgetRepo.DeleteAsync(widget, ct);

        logger.LogInformation("Widget deleted: {WidgetId}", request.Id);
        return OperationCode.Done;
    }
}