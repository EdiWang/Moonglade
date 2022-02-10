namespace Moonglade.Core.PageFeature;

public record DeletePageCommand(Guid Id) : IRequest;

public class DeletePageCommandHandler : IRequestHandler<DeletePageCommand>
{
    private readonly IRepository<PageEntity> _pageRepo;

    public DeletePageCommandHandler(IRepository<PageEntity> pageRepo)
    {
        _pageRepo = pageRepo;
    }

    public async Task<Unit> Handle(DeletePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _pageRepo.GetAsync(request.Id);
        if (page is null)
        {
            throw new InvalidOperationException($"CustomPageEntity with Id '{request.Id}' not found.");
        }

        await _pageRepo.DeleteAsync(request.Id);

        return Unit.Value;
    }
}