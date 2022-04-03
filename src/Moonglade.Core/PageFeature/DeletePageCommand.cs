namespace Moonglade.Core.PageFeature;

public record DeletePageCommand(Guid Id) : IRequest;

public class DeletePageCommandHandler : AsyncRequestHandler<DeletePageCommand>
{
    private readonly IRepository<PageEntity> _pageRepo;
    public DeletePageCommandHandler(IRepository<PageEntity> pageRepo) => _pageRepo = pageRepo;


    protected override async Task Handle(DeletePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _pageRepo.GetAsync(request.Id);
        if (page is null)
        {
            throw new InvalidOperationException($"CustomPageEntity with Id '{request.Id}' not found.");
        }

        await _pageRepo.DeleteAsync(request.Id, cancellationToken);
    }
}