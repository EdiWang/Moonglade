namespace Moonglade.Core;

public record DeleteStyleSheetCommand(Guid Id) : IRequest;

public class DeleteStyleSheetCommandHandler : IRequestHandler<DeleteStyleSheetCommand>
{
    private readonly IRepository<StyleSheetEntity> _repo;

    public DeleteStyleSheetCommandHandler(IRepository<StyleSheetEntity> repo) => _repo = repo;

    public async Task Handle(DeleteStyleSheetCommand request, CancellationToken cancellationToken)
    {
        var styleSheet = await _repo.GetAsync(request.Id, cancellationToken);
        if (styleSheet is null)
        {
            throw new InvalidOperationException($"StyleSheetEntity with Id '{request.Id}' not found.");
        }

        await _repo.DeleteAsync(styleSheet, cancellationToken);
    }
}