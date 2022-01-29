using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Menus;

public record GetMenuQuery(Guid Id) : IRequest<Menu>;

public class GetMenuQueryHandler : IRequestHandler<GetMenuQuery, Menu>
{
    private readonly IRepository<MenuEntity> _menuRepo;

    public GetMenuQueryHandler(IRepository<MenuEntity> menuRepo)
    {
        _menuRepo = menuRepo;
    }

    public async Task<Menu> Handle(GetMenuQuery request, CancellationToken cancellationToken)
    {
        var entity = await _menuRepo.GetAsync(request.Id);
        if (null == entity) return null;

        var item = new Menu(entity);
        return item;
    }
}