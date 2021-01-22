using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Core
{
    public interface IMenuService
    {
        Task<Menu> GetAsync(Guid id);
        Task<IReadOnlyList<Menu>> GetAllAsync();
        Task<Guid> CreateAsync(CreateMenuRequest request);
        Task<Guid> UpdateAsync(EditMenuRequest request);
        Task DeleteAsync(Guid id);
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

        public async Task<Menu> GetAsync(Guid id)
        {
            var entity = await _menuRepo.GetAsync(id);
            var item = EntityToMenuModel(entity);
            return item;
        }

        public Task<IReadOnlyList<Menu>> GetAllAsync()
        {
            var list = _menuRepo.SelectAsync(p => new Menu
            {
                Id = p.Id,
                DisplayOrder = p.DisplayOrder,
                Icon = p.Icon,
                Title = p.Title,
                Url = p.Url,
                IsOpenInNewTab = p.IsOpenInNewTab
            });

            return list;
        }

        public async Task<Guid> CreateAsync(CreateMenuRequest request)
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

            await _menuRepo.AddAsync(menu);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.MenuCreated, $"Menu '{menu.Id}' created.");

            return uid;
        }

        public async Task<Guid> UpdateAsync(EditMenuRequest request)
        {
            var menu = await _menuRepo.GetAsync(request.Id);
            if (menu is null)
            {
                throw new InvalidOperationException($"MenuEntity with Id '{request.Id}' not found.");
            }

            var url = Helper.SterilizeLink(request.Url.Trim());
            _logger.LogInformation($"Sterilized URL from '{request.Url}' to '{url}'");

            menu.Title = request.Title.Trim();
            menu.Url = url;
            menu.DisplayOrder = request.DisplayOrder;
            menu.Icon = request.Icon;
            menu.IsOpenInNewTab = request.IsOpenInNewTab;

            await _menuRepo.UpdateAsync(menu);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.MenuUpdated, $"Menu '{request.Id}' updated.");

            return menu.Id;
        }

        public async Task DeleteAsync(Guid id)
        {
            var menu = await _menuRepo.GetAsync(id);
            if (menu is null)
            {
                throw new InvalidOperationException($"MenuEntity with Id '{id}' not found.");
            }

            _menuRepo.Delete(id);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.CategoryDeleted, $"Menu '{id}' deleted.");
        }

        private static Menu EntityToMenuModel(MenuEntity entity)
        {
            if (entity is null) return null;

            return new()
            {
                Id = entity.Id,
                Title = entity.Title.Trim(),
                DisplayOrder = entity.DisplayOrder,
                Icon = entity.Icon.Trim(),
                Url = entity.Url.Trim(),
                IsOpenInNewTab = entity.IsOpenInNewTab
            };
        }
    }
}
