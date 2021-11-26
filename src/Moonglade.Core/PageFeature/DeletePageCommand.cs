using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.PageFeature;

public class DeletePageCommand : IRequest
{
    public DeletePageCommand(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; set; }
}

public class DeletePageCommandHandler : IRequestHandler<DeletePageCommand>
{
    private readonly IRepository<PageEntity> _pageRepo;
    private readonly IBlogAudit _audit;

    public DeletePageCommandHandler(IRepository<PageEntity> pageRepo, IBlogAudit audit)
    {
        _pageRepo = pageRepo;
        _audit = audit;
    }

    public async Task<Unit> Handle(DeletePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _pageRepo.GetAsync(request.Id);
        if (page is null)
        {
            throw new InvalidOperationException($"CustomPageEntity with Id '{request.Id}' not found.");
        }

        await _pageRepo.DeleteAsync(request.Id);
        await _audit.AddEntry(BlogEventType.Content, BlogEventId.PageDeleted, $"Page '{request.Id}' deleted.");

        return Unit.Value;
    }
}