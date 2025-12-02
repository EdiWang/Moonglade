using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public class UpdateWidgetCommand : CreateWidgetCommand, ICommand<OperationCode>
{
    public Guid Id { get; set; }
}

public class UpdateWidgetCommandHandler(
    MoongladeRepository<WidgetEntity> widgetRepo,
    ILogger<UpdateWidgetCommandHandler> logger) : ICommandHandler<UpdateWidgetCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateWidgetCommand request, CancellationToken ct)
    {
        var widget = await widgetRepo.GetByIdAsync(request.Id, ct);
        if (widget is null) return OperationCode.ObjectNotFound;

        widget.Title = request.Title.Trim();
        widget.WidgetType = request.WidgetType.Trim();
        widget.DisplayOrder = request.DisplayOrder;
        widget.IsEnabled = request.IsEnabled;

        await widgetRepo.UpdateAsync(widget, ct);

        logger.LogInformation("Widget updated: {WidgetId}", widget.Id);
        return OperationCode.Done;
    }
}