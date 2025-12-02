using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Utils;

namespace Moonglade.Widgets.Types.LinkList;

public record UpdateWidgetLinkItemCommand(Guid Id, EditWidgetLinkItemRequest Payload) : ICommand<OperationCode>;

public class UpdateWidgetLinkItemCommandHandler(
    MoongladeRepository<WidgetLinkItemEntity> linkItemRepo,
    ILogger<UpdateWidgetLinkItemCommandHandler> logger) : ICommandHandler<UpdateWidgetLinkItemCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateWidgetLinkItemCommand request, CancellationToken ct)
    {
        var linkItem = await linkItemRepo.GetByIdAsync(request.Id, ct);
        if (linkItem is null)
        {
            return OperationCode.ObjectNotFound;
        }

        linkItem.Title = request.Payload.Title.Trim();
        linkItem.Url = SecurityHelper.SterilizeLink(request.Payload.Url);
        linkItem.IconName = request.Payload.IconName?.Trim() ?? string.Empty;
        linkItem.OpenInNewWindow = request.Payload.OpenInNewWindow;
        linkItem.DisplayOrder = request.Payload.DisplayOrder;
        linkItem.IsEnabled = request.Payload.IsEnabled;

        await linkItemRepo.UpdateAsync(linkItem, ct);

        logger.LogInformation("Widget link item updated: {LinkItemId}", request.Id);

        return OperationCode.Done;
    }
}