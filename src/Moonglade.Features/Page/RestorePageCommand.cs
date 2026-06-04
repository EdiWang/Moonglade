using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Page;

public record RestorePageCommand(Guid Id) : ICommand;

public class RestorePageCommandHandler(
    BlogDbContext db,
    ILogger<RestorePageCommandHandler> logger) : ICommandHandler<RestorePageCommand>
{
    public async Task HandleAsync(RestorePageCommand request, CancellationToken ct)
    {
        var page = await db.BlogPage.FindAsync([request.Id], ct);
        if (page == null) return;

        page.IsDeleted = false;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Page [{PageId}] restored", request.Id);
    }
}
