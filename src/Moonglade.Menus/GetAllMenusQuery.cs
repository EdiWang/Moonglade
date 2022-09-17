using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Menus;

public record GetAllMenusQuery : IRequest<IReadOnlyList<Menu>>;

public class GetAllMenusQueryHandler : IRequestHandler<GetAllMenusQuery, IReadOnlyList<Menu>>
{
    private readonly IRepository<MenuEntity> _repo;

    public GetAllMenusQueryHandler(IRepository<MenuEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<Menu>> Handle(GetAllMenusQuery request, CancellationToken ct)
    {
        var list = _repo.SelectAsync(p => new Menu
        {
            Id = p.Id,
            DisplayOrder = p.DisplayOrder,
            Icon = p.Icon,
            Title = p.Title,
            Url = p.Url,
            IsOpenInNewTab = p.IsOpenInNewTab,
            SubMenus = p.SubMenus.Select(sm => new SubMenu
            {
                Id = sm.Id,
                Title = sm.Title,
                Url = sm.Url,
                IsOpenInNewTab = sm.IsOpenInNewTab
            }).ToList()
        });

        return list;
    }
}