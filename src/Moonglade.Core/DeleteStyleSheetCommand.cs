namespace Moonglade.Core;

public record DeleteStyleSheetCommand(Guid Id) : IRequest;

public class DeleteStyleSheetCommandHandler(IRepository<StyleSheetEntity> repo) : IRequestHandler<DeleteStyleSheetCommand>
{
    public async Task Handle(DeleteStyleSheetCommand request, CancellationToken cancellationToken)
    {
        var styleSheet = await repo.GetAsync(request.Id, cancellationToken);
        if (styleSheet is null)
        {
            throw new InvalidOperationException($"StyleSheetEntity with Id '{request.Id}' not found.");
        }

        await repo.DeleteAsync(styleSheet, cancellationToken);
    }
}