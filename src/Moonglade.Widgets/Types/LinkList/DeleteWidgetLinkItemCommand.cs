using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets.Types.LinkList;

public record DeleteWidgetLinkItemCommand(Guid Id) : ICommand<OperationCode>;

public class DeleteWidgetLinkItemCommandHandler(
    MoongladeRepository<WidgetLinkItemEntity> linkItemRepo,
    ILogger<DeleteWidgetLinkItemCommandHandler> logger) : ICommandHandler<DeleteWidgetLinkItemCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteWidgetLinkItemCommand request, CancellationToken ct)
    {
        var linkItem = await linkItemRepo.GetByIdAsync(request.Id, ct);
        if (linkItem is null)
        {
            return OperationCode.ObjectNotFound;
        }

        await linkItemRepo.DeleteAsync(linkItem, ct);

        logger.LogInformation("Widget link item deleted: {LinkItemId}", request.Id);

        return OperationCode.Done;
    }
}