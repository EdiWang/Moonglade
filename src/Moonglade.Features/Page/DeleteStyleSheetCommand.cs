using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Page;

public record DeleteStyleSheetCommand(Guid Id) : ICommand;

public class DeleteStyleSheetCommandHandler(
    BlogDbContext db,
    ILogger<DeleteStyleSheetCommandHandler> logger
    ) : ICommandHandler<DeleteStyleSheetCommand>
{
    public async Task HandleAsync(DeleteStyleSheetCommand request, CancellationToken ct)
    {
        var styleSheet = await db.StyleSheet.FindAsync([request.Id], ct);
        if (styleSheet is null)
        {
            throw new InvalidOperationException($"StyleSheetEntity with Id '{request.Id}' not found.");
        }

        db.StyleSheet.Remove(styleSheet);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deleted StyleSheetEntity with Id '{Id}'", request.Id);
    }
}