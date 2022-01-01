using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Menus;

public class CreateMenuCommand : IRequest<Guid>
{
    public CreateMenuCommand(EditMenuRequest payload)
    {
        Payload = payload;
    }

    public EditMenuRequest Payload { get; set; }
}

public class CreateMenuCommandHandler : IRequestHandler<CreateMenuCommand, Guid>
{
    private readonly IRepository<MenuEntity> _menuRepo;

    public CreateMenuCommandHandler(IRepository<MenuEntity> menuRepo)
    {
        _menuRepo = menuRepo;
    }

    public async Task<Guid> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
    {
        var uid = Guid.NewGuid();
        var menu = new MenuEntity
        {
            Id = uid,
            Title = request.Payload.Title.Trim(),
            DisplayOrder = request.Payload.DisplayOrder.GetValueOrDefault(),
            Icon = request.Payload.Icon,
            Url = request.Payload.Url,
            IsOpenInNewTab = request.Payload.IsOpenInNewTab
        };

        if (request.Payload.SubMenus is { Length: > 0 })
        {
            var sms = request.Payload.SubMenus.Select(p => new SubMenuEntity
            {
                Id = Guid.NewGuid(),
                IsOpenInNewTab = p.IsOpenInNewTab,
                Title = p.Title,
                Url = p.Url,
                MenuId = uid
            });

            menu.SubMenus = sms.ToList();
        }

        await _menuRepo.AddAsync(menu);
        return uid;
    }
}