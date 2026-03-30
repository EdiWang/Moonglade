using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Moonglade.Widgets;

public record DeleteWidgetCommand(Guid Id) : ICommand<OperationCode>;

public class DeleteWidgetCommandHandler(
    BlogDbContext db,
    ILogger<DeleteWidgetCommandHandler> logger) : ICommandHandler<DeleteWidgetCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteWidgetCommand request, CancellationToken ct)
    {
        var widget = await db.Widget.FindAsync([request.Id], ct);
        if (widget is null) return OperationCode.ObjectNotFound;

        db.Widget.Remove(widget);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Widget deleted: {WidgetId}", request.Id);
        return OperationCode.Done;
    }
}