using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Menus
{
    public class UpdateMenuCommand : IRequest
    {
        public UpdateMenuCommand(Guid id, UpdateMenuRequest request)
        {
            Id = id;
            Request = request;
        }

        public Guid Id { get; set; }
        public UpdateMenuRequest Request { get; set; }
    }

    public class UpdateMenuCommandHandler : IRequestHandler<UpdateMenuCommand>
    {
        private readonly IRepository<MenuEntity> _menuRepo;
        private readonly IBlogAudit _audit;

        public UpdateMenuCommandHandler(IRepository<MenuEntity> menuRepo, IBlogAudit audit)
        {
            _menuRepo = menuRepo;
            _audit = audit;
        }

        public async Task<Unit> Handle(UpdateMenuCommand request, CancellationToken cancellationToken)
        {
            var menu = await _menuRepo.GetAsync(request.Id);
            if (menu is null)
            {
                throw new InvalidOperationException($"MenuEntity with Id '{request.Id}' not found.");
            }

            var url = Helper.SterilizeLink(request.Request.Url.Trim());

            menu.Title = request.Request.Title.Trim();
            menu.Url = url;
            menu.DisplayOrder = request.Request.DisplayOrder;
            menu.Icon = request.Request.Icon;
            menu.IsOpenInNewTab = request.Request.IsOpenInNewTab;

            if (request.Request.SubMenus is { Length: > 0 })
            {
                menu.SubMenus.Clear();
                var sms = request.Request.SubMenus.Select(p => new SubMenuEntity
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
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.MenuUpdated, $"Menu '{request.Id}' updated.");

            return Unit.Value;
        }
    }
}
