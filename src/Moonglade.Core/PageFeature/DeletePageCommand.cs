namespace Moonglade.Core.PageFeature;

public record DeletePageCommand(Guid Id) : IRequest;

public class DeletePageCommandHandler : AsyncRequestHandler<DeletePageCommand>
{
    private readonly IRepository<PageEntity> _repo;
    public DeletePageCommandHandler(IRepository<PageEntity> repo) => _repo = repo;

    protected override async Task Handle(DeletePageCommand request, CancellationToken ct) =>
        await _repo.DeleteAsync(request.Id, ct);
}