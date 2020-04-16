using System;
using System.Collections.Generic;
using System.Text;
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

        public Task<Response<IReadOnlyList<MenuModel>>> GetAllMenus()
        {
            return TryExecuteAsync<IReadOnlyList<MenuModel>>(async () =>
            {
                var list = await _menuRepository.SelectAsync(p => new MenuModel
                {
                    Id = p.Id,
                    DisplayOrder = p.DisplayOrder,
                    Icon = p.Icon,
                    Title = p.Title,
                    Url = p.Url
                });
                return new SuccessResponse<IReadOnlyList<MenuModel>>(list);
            });
        }

        public Response DeleteMenu(Guid id)
        {
            return TryExecute(() =>
            {
                var menu = _menuRepository.Get(id);
                if (null == menu)
                {
                    throw new InvalidOperationException($"MenuEntity with Id '{id}' is not found.");
                }

                _menuRepository.Delete(id);
                return new SuccessResponse();
            });
        }

        private MenuModel EntityToMenuModel(MenuEntity entity)
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
                Url = entity.Url.Trim()
            };
        }
    }
}
