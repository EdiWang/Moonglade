using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Menus;

public class DeleteMenuCommand : IRequest
{
    public DeleteMenuCommand(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; set; }
}

public class DeleteMenuCommandHandler : IRequestHandler<DeleteMenuCommand>
{
    private readonly IRepository<MenuEntity> _menuRepo;

    public DeleteMenuCommandHandler(IRepository<MenuEntity> menuRepo)
    {
        _menuRepo = menuRepo;
    }

    public async Task<Unit> Handle(DeleteMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = await _menuRepo.GetAsync(request.Id);
        if (menu is null)
        {
            throw new InvalidOperationException($"MenuEntity with Id '{request.Id}' not found.");
        }

        await _menuRepo.DeleteAsync(request.Id);

        return Unit.Value;
    }
}