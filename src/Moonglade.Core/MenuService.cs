using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class MenuService : BlogService
    {
        private readonly IRepository<MenuEntity> _menuRepository;
        private readonly IBlogAudit _blogAudit;

        public MenuService(
            ILogger<MenuService> logger,
            IOptions<AppSettings> settings,
            IRepository<MenuEntity> menuRepository,
            IBlogAudit blogAudit) : base(logger, settings)
        {
            _menuRepository = menuRepository;
            _blogAudit = blogAudit;
        }

        public Task<Response<Menu>> GetAsync(Guid id)
        {
            return TryExecuteAsync<Menu>(async () =>
            {
                var entity = await _menuRepository.GetAsync(id);
                var item = EntityToMenuModel(entity);
                return new SuccessResponse<Menu>(item);
            });
        }

        public Task<Response<IReadOnlyList<Menu>>> GetAllAsync()
        {
            return TryExecuteAsync<IReadOnlyList<Menu>>(async () =>
            {
                var list = await _menuRepository.SelectAsync(p => new Menu
                {
                    Id = p.Id,
                    DisplayOrder = p.DisplayOrder,
                    Icon = p.Icon,
                    Title = p.Title,
                    Url = p.Url,
                    IsOpenInNewTab = p.IsOpenInNewTab
                });
                return new SuccessResponse<IReadOnlyList<Menu>>(list);
            });
        }

        public Task<Response<Guid>> CreateAsync(CreateMenuRequest request)
        {
            return TryExecuteAsync<Guid>(async () =>
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

                await _menuRepository.AddAsync(menu);
                await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.MenuCreated, $"Menu '{menu.Id}' created.");

                return new SuccessResponse<Guid>(uid);
            });
        }

        public Task<Response<Guid>> UpdateAsync(EditMenuRequest request)
        {
            return TryExecuteAsync<Guid>(async () =>
            {
                var menu = await _menuRepository.GetAsync(request.Id);
                if (null == menu)
                {
                    throw new InvalidOperationException($"MenuEntity with Id '{request.Id}' not found.");
                }

                var sUrl = Utils.SterilizeMenuLink(request.Url.Trim());
                Logger.LogInformation($"Sterilized URL from '{request.Url}' to '{sUrl}'");

                menu.Title = request.Title.Trim();
                menu.Url = sUrl;
                menu.DisplayOrder = request.DisplayOrder;
                menu.Icon = request.Icon;
                menu.IsOpenInNewTab = request.IsOpenInNewTab;

                await _menuRepository.UpdateAsync(menu);
                await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.MenuUpdated, $"Menu '{request.Id}' updated.");

                return new SuccessResponse<Guid>(menu.Id);
            });
        }

        public Task<Response> DeleteAsync(Guid id)
        {
            return TryExecuteAsync(async () =>
            {
                var menu = await _menuRepository.GetAsync(id);
                if (null == menu)
                {
                    throw new InvalidOperationException($"MenuEntity with Id '{id}' not found.");
                }
                
                _menuRepository.Delete(id);
                await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.CategoryDeleted, $"Menu '{id}' deleted.");
                
                return new SuccessResponse();
            });
        }

        private static Menu EntityToMenuModel(MenuEntity entity)
        {
            if (null == entity)
            {
                return null;
            }

            return new Menu
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
