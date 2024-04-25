using Moonglade.Data;

namespace Moonglade.Core;

public record DeleteStyleSheetCommand(Guid Id) : IRequest;

public class DeleteStyleSheetCommandHandler(MoongladeRepository<StyleSheetEntity> repo) : IRequestHandler<DeleteStyleSheetCommand>
{
    public async Task Handle(DeleteStyleSheetCommand request, CancellationToken ct)
    {
        var styleSheet = await repo.GetByIdAsync(request.Id, ct);
        if (styleSheet is null)
        {
            throw new InvalidOperationException($"StyleSheetEntity with Id '{request.Id}' not found.");
        }

        await repo.DeleteAsync(styleSheet, ct);
    }
}