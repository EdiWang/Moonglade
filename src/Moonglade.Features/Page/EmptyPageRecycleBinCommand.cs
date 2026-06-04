using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Page;

public record EmptyPageRecycleBinCommand : ICommand<string[]>;

public class EmptyPageRecycleBinCommandHandler(
    BlogDbContext db,
    ICommandMediator commandMediator,
    ILogger<EmptyPageRecycleBinCommandHandler> logger) : ICommandHandler<EmptyPageRecycleBinCommand, string[]>
{
    public async Task<string[]> HandleAsync(EmptyPageRecycleBinCommand request, CancellationToken ct)
    {
        var deletedPages = await db.BlogPage
            .Where(p => p.IsDeleted)
            .Select(p => new { p.Slug, p.CssId })
            .ToArrayAsync(ct);

        foreach (var page in deletedPages.Where(p => !string.IsNullOrWhiteSpace(p.CssId)))
        {
            await commandMediator.SendAsync(new DeleteStyleSheetCommand(new(page.CssId)), ct);
        }

        await db.BlogPage.Where(p => p.IsDeleted).ExecuteDeleteAsync(ct);

        logger.LogInformation("Page recycle bin emptied.");

        return deletedPages.Select(p => p.Slug).ToArray();
    }
}
