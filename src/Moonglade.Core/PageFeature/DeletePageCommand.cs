namespace Moonglade.Core.PageFeature;

public record DeletePageCommand(Guid Id) : IRequest;

public class DeletePageCommandHandler : IRequestHandler<DeletePageCommand>
{
    private readonly IRepository<PageEntity> _repo;
    private readonly IMediator _mediator;

    public DeletePageCommandHandler(IRepository<PageEntity> repo, IMediator mediator)
    {
        _repo = repo;
        _mediator = mediator;
    }

    public async Task Handle(DeletePageCommand request, CancellationToken ct)
    {
        var page = await _repo.GetAsync(request.Id, ct);
        if (page is null)
        {
            throw new InvalidOperationException($"PageEntity with Id '{request.Id}' not found.");
        }

        if (page.CssId != null)
        {
            await _mediator.Send(new DeleteStyleSheetCommand(new(page.CssId)), ct);
        }

        await _repo.DeleteAsync(request.Id, ct);
    }
}