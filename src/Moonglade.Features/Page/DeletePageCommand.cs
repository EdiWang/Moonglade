using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Page;

public record DeletePageCommand(Guid Id) : ICommand<OperationCode>;

public class DeletePageCommandHandler(
    BlogDbContext db,
    ICommandMediator commandMediator,
    ILogger<DeletePageCommandHandler> logger) : ICommandHandler<DeletePageCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeletePageCommand request, CancellationToken ct)
    {
        var page = await db.BlogPage.FindAsync([request.Id], ct);
        if (page == null) return OperationCode.ObjectNotFound;

        if (!string.IsNullOrWhiteSpace(page.CssId))
        {
            await commandMediator.SendAsync(new DeleteStyleSheetCommand(new(page.CssId)), ct);
        }

        db.BlogPage.Remove(page);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deleted page: {PageId}", request.Id);
        return OperationCode.Done;
    }
}