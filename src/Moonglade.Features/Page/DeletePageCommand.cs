using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Features.Page;

public record DeletePageCommand(Guid Id) : ICommand<OperationCode>;

public class DeletePageCommandHandler(
    MoongladeRepository<PageEntity> repo,
    ICommandMediator commandMediator,
    ILogger<DeletePageCommandHandler> logger) : ICommandHandler<DeletePageCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeletePageCommand request, CancellationToken ct)
    {
        var page = await repo.GetByIdAsync(request.Id, ct);
        if (page == null) return OperationCode.ObjectNotFound;

        if (!string.IsNullOrWhiteSpace(page.CssId))
        {
            await commandMediator.SendAsync(new DeleteStyleSheetCommand(new(page.CssId)), ct);
        }

        await repo.DeleteAsync(page, ct);

        logger.LogInformation("Deleted page: {PageId}", request.Id);
        return OperationCode.Done;
    }
}