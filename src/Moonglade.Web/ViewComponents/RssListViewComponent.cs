using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class RssListViewComponent : ViewComponent
    {
        private readonly ILogger<RssListViewComponent> _logger;

        private readonly ICategoryService _catService;

        public RssListViewComponent(ILogger<RssListViewComponent> logger, ICategoryService catService)
        {
            _logger = logger;
            _catService = catService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var cats = await _catService.GetAllAsync();
                var items = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));

                return View(items);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error.");
                return Content(e.Message);
            }
        }
    }
}
