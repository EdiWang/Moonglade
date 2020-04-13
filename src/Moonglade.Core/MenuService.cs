using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class MenuService : MoongladeService
    {
        private readonly IRepository<MenuEntity> _menuRepository;

        public MenuService(
            ILogger<MenuService> logger,
            IOptions<AppSettings> settings, 
            IRepository<MenuEntity> menuRepository) : base(logger, settings)
        {
            _menuRepository = menuRepository;
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
    }
}
