using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Menus
{
    public interface IMenuService
    {
        Task<Guid> CreateAsync(UpdateMenuRequest request);
        Task UpdateAsync(Guid id, UpdateMenuRequest request);
    }

    public class MenuService : IMenuService
    {
        private readonly ILogger<MenuService> _logger;
        private readonly IRepository<MenuEntity> _menuRepo;
        private readonly IBlogAudit _audit;

        public MenuService(
            ILogger<MenuService> logger,
            IRepository<MenuEntity> menuRepo,
            IBlogAudit audit)
        {
            _logger = logger;
            _menuRepo = menuRepo;
            _audit = audit;
        }

        public async Task<Guid> CreateAsync(UpdateMenuRequest request)
        {
            var uid = Guid.NewGuid();
            var menu = new MenuEntity
            {
                Id = uid,
                Title = request.Title.Trim(),
                DisplayOrder = request.DisplayOrder,
                Icon = request.Icon,
                Url = request.Url,
                IsOpenInNewTab = request.IsOpenInNewTab
            };

            if (request.SubMenus is { Length: > 0 })
            {
                var sms = request.SubMenus.Select(p => new SubMenuEntity
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
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.MenuCreated, $"Menu '{uid}' created.");
            return uid;
        }

        public async Task UpdateAsync(Guid id, UpdateMenuRequest request)
        {
            var menu = await _menuRepo.GetAsync(id);
            if (menu is null)
            {
                throw new InvalidOperationException($"MenuEntity with Id '{id}' not found.");
            }

            var url = Helper.SterilizeLink(request.Url.Trim());
            _logger.LogInformation($"Sterilized URL from '{request.Url}' to '{url}'");

            menu.Title = request.Title.Trim();
            menu.Url = url;
            menu.DisplayOrder = request.DisplayOrder;
            menu.Icon = request.Icon;
            menu.IsOpenInNewTab = request.IsOpenInNewTab;

            if (request.SubMenus is { Length: > 0 })
            {
                menu.SubMenus.Clear();
                var sms = request.SubMenus.Select(p => new SubMenuEntity
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
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.MenuUpdated, $"Menu '{id}' updated.");
        }
    }
}
