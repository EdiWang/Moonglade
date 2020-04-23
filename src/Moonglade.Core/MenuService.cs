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
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Core
{
    public class MenuService : MoongladeService
    {
        private readonly IRepository<MenuEntity> _menuRepository;
        private readonly IMoongladeAudit _moongladeAudit;

        public MenuService(
            ILogger<MenuService> logger,
            IOptions<AppSettings> settings,
            IRepository<MenuEntity> menuRepository,
            IMoongladeAudit moongladeAudit) : base(logger, settings)
        {
            _menuRepository = menuRepository;
            _moongladeAudit = moongladeAudit;
        }

        public Task<Response<MenuModel>> GetMenuAsync(Guid id)
        {
            return TryExecuteAsync<MenuModel>(async () =>
            {
                var entity = await _menuRepository.GetAsync(id);
                var item = EntityToMenuModel(entity);
                return new SuccessResponse<MenuModel>(item);
            });
        }

        public Task<Response<IReadOnlyList<MenuModel>>> GetAllMenusAsync()
        {
            return TryExecuteAsync<IReadOnlyList<MenuModel>>(async () =>
            {
                var list = await _menuRepository.SelectAsync(p => new MenuModel
                {
                    Id = p.Id,
                    DisplayOrder = p.DisplayOrder,
                    Icon = p.Icon,
                    Title = p.Title,
                    Url = p.Url,
                    IsOpenInNewTab = p.IsOpenInNewTab
                });
                return new SuccessResponse<IReadOnlyList<MenuModel>>(list);
            });
        }

        public Task<Response<Guid>> CreateMenuAsync(CreateMenuRequest request)
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
                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.MenuCreated, $"Menu '{menu.Id}' created.");

                return new SuccessResponse<Guid>(uid);
            });
        }

        public Task<Response<Guid>> UpdateMenuAsync(EditMenuRequest request)
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
                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.MenuUpdated, $"Menu '{request.Id}' updated.");

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
                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.CategoryDeleted, $"Menu '{id}' deleted.");
                
                return new SuccessResponse();
            });
        }

        private static MenuModel EntityToMenuModel(MenuEntity entity)
        {
            if (null == entity)
            {
                return null;
            }

            return new MenuModel
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
