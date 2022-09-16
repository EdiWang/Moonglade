using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Menus;

public record UpdateMenuCommand(EditMenuRequest Payload) : IRequest;

public class UpdateMenuCommandHandler : AsyncRequestHandler<UpdateMenuCommand>
{
    private readonly IRepository<MenuEntity> _repo;

    public UpdateMenuCommandHandler(IRepository<MenuEntity> repo) => _repo = repo;

    protected override async Task Handle(UpdateMenuCommand request, CancellationToken ct)
    {
        var menu = await _repo.GetAsync(request.Payload.Id, ct);
        if (menu is null)
        {
            throw new InvalidOperationException($"MenuEntity with Id '{request.Payload.Id}' not found.");
        }

        var url = Helper.SterilizeLink(request.Payload.Url.Trim());

        menu.Title = request.Payload.Title.Trim();
        menu.Url = url;
        menu.DisplayOrder = request.Payload.DisplayOrder.GetValueOrDefault();
        menu.Icon = request.Payload.Icon;
        menu.IsOpenInNewTab = request.Payload.IsOpenInNewTab;

        if (request.Payload.SubMenus != null)
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

        await _repo.UpdateAsync(menu, ct);
    }
}