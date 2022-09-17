using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Menus;

public record DeleteMenuCommand(Guid Id) : IRequest;

public class DeleteMenuCommandHandler : AsyncRequestHandler<DeleteMenuCommand>
{
    private readonly IRepository<MenuEntity> _repo;

    public DeleteMenuCommandHandler(IRepository<MenuEntity> repo) => _repo = repo;

    protected override async Task Handle(DeleteMenuCommand request, CancellationToken ct)
    {
        var menu = await _repo.GetAsync(request.Id, ct);
        if (menu != null) await _repo.DeleteAsync(request.Id, ct);
    }
}