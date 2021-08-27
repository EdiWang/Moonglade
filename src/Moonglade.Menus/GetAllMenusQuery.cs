using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Menus
{
    public class GetAllMenusQuery : IRequest<IReadOnlyList<Menu>>
    {
    }

    public class GetAllMenusQueryHandler : IRequestHandler<GetAllMenusQuery, IReadOnlyList<Menu>>
    {
        private readonly IRepository<MenuEntity> _menuRepo;

        public GetAllMenusQueryHandler(IRepository<MenuEntity> menuRepo)
        {
            _menuRepo = menuRepo;
        }

        public Task<IReadOnlyList<Menu>> Handle(GetAllMenusQuery request, CancellationToken cancellationToken)
        {
            var list = _menuRepo.SelectAsync(p => new Menu
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
}
