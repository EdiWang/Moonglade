using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Menus;

public record UpdateMenuCommand(Guid Id, EditMenuRequest Payload) : IRequest;

public class UpdateMenuCommandHandler : IRequestHandler<UpdateMenuCommand>
{
    private readonly IRepository<MenuEntity> _menuRepo;

    public UpdateMenuCommandHandler(IRepository<MenuEntity> menuRepo)
    {
        _menuRepo = menuRepo;
    }

    public async Task<Unit> Handle(UpdateMenuCommand request, CancellationToken cancellationToken)
    {
        var (guid, payload) = request;
        var menu = await _menuRepo.GetAsync(guid);
        if (menu is null)
        {
            throw new InvalidOperationException($"MenuEntity with Id '{guid}' not found.");
        }

        var url = Helper.SterilizeLink(payload.Url.Trim());

        menu.Title = payload.Title.Trim();
        menu.Url = url;
        menu.DisplayOrder = payload.DisplayOrder.GetValueOrDefault();
        menu.Icon = payload.Icon;
        menu.IsOpenInNewTab = payload.IsOpenInNewTab;

        if (payload.SubMenus != null)
        {
            menu.SubMenus.Clear();
            var sms = payload.SubMenus.Select(p => new SubMenuEntity
            {
                Id = Guid.NewGuid(),
                IsOpenInNewTab = p.IsOpenInNewTab,
                Title = p.Title,
                Url = p.Url,
                MenuId = menu.Id
            });

            menu.SubMenus = sms.ToList();
        }

        await _menuRepo.UpdateAsync(menu);

        return Unit.Value;
    }
}