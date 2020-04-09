using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
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
    }
}
