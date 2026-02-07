using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features;

public record DeleteStyleSheetCommand(Guid Id) : ICommand;

public class DeleteStyleSheetCommandHandler(
    IRepositoryBase<StyleSheetEntity> repo,
    ILogger<DeleteStyleSheetCommandHandler> logger
    ) : ICommandHandler<DeleteStyleSheetCommand>
{
    public async Task HandleAsync(DeleteStyleSheetCommand request, CancellationToken ct)
    {
        var styleSheet = await repo.GetByIdAsync(request.Id, ct);
        if (styleSheet is null)
        {
            throw new InvalidOperationException($"StyleSheetEntity with Id '{request.Id}' not found.");
        }

        await repo.DeleteAsync(styleSheet, ct);

        logger.LogInformation("Deleted StyleSheetEntity with Id '{Id}'", request.Id);
    }
}