using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets;

public record UpdateWidgetCommand(Guid Id, EditWidgetRequest Payload) : ICommand<OperationCode>;

public class UpdateWidgetCommandHandler(
    BlogDbContext db,
    ILogger<UpdateWidgetCommandHandler> logger,
    WidgetCollectionGate gate) : ICommandHandler<UpdateWidgetCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateWidgetCommand request, CancellationToken ct)
    {
        var result = await gate.ExecuteAsync(async token =>
        {
            var widget = await db.Widget.FindAsync([request.Id], token);
            if (widget is null) return OperationCode.ObjectNotFound;

            Validator.ValidateObject(request.Payload, new ValidationContext(request.Payload), true);
            widget.Title = request.Payload.Title.Trim();
            widget.WidgetType = request.Payload.WidgetType;
            widget.ContentType = WidgetContentType.JSON; // Currently only JSON is supported
            widget.ContentCode = request.Payload.ContentCode?.Trim();
            widget.Region = request.Payload.Region;
            widget.SortOrder = request.Payload.SortOrder;
            widget.IsEnabled = request.Payload.IsEnabled;

            await db.SaveChangesAsync(token);
            return OperationCode.Done;
        }, ct);

        if (result == OperationCode.Done) logger.LogInformation("Widget updated: {WidgetId}", request.Id);
        return result;
    }
}
