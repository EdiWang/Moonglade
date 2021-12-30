using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Menus;

public class UpdateMenuCommand : IRequest
{
    public UpdateMenuCommand(Guid id, EditMenuRequest payload)
    {
        Id = id;
        Payload = payload;
    }

    public Guid Id { get; set; }
    public EditMenuRequest Payload { get; set; }
}

public class UpdateMenuCommandHandler : IRequestHandler<UpdateMenuCommand>
{
    private readonly IRepository<MenuEntity> _menuRepo;

    public UpdateMenuCommandHandler(IRepository<MenuEntity> menuRepo)
    {
        _menuRepo = menuRepo;
    }

    public async Task<Unit> Handle(UpdateMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = await _menuRepo.GetAsync(request.Id);
        if (menu is null)
        {
            throw new InvalidOperationException($"MenuEntity with Id '{request.Id}' not found.");
        }

        var url = Helper.SterilizeLink(request.Payload.Url.Trim());

        menu.Title = request.Payload.Title.Trim();
        menu.Url = url;
        menu.DisplayOrder = request.Payload.DisplayOrder.GetValueOrDefault();
        menu.Icon = request.Payload.Icon;
        menu.IsOpenInNewTab = request.Payload.IsOpenInNewTab;

        if (request.Payload.SubMenus is { Length: > 0 })
        {
            menu.SubMenus.Clear();
            var sms = request.Payload.SubMenus.Select(p => new SubMenuEntity
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