using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Widgets;

public record DeleteWidgetCommand(Guid Id) : ICommand<OperationCode>;

public class DeleteWidgetCommandHandler(
    BlogDbContext db,
    ILogger<DeleteWidgetCommandHandler> logger,
    WidgetCollectionGate gate) : ICommandHandler<DeleteWidgetCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteWidgetCommand request, CancellationToken ct)
    {
        var result = await gate.ExecuteAsync(async token =>
        {
            var widget = await db.Widget.FindAsync([request.Id], token);
            if (widget is null) return OperationCode.ObjectNotFound;

            db.Widget.Remove(widget);
            await db.SaveChangesAsync(token);
            return OperationCode.Done;
        }, ct);

        if (result == OperationCode.Done) logger.LogInformation("Widget deleted: {WidgetId}", request.Id);
        return result;
    }
}
