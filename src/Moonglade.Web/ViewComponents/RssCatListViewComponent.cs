using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;

namespace Moonglade.Web.ViewComponents
{
    public class RssCatListViewComponent : MoongladeViewComponent
    {
        private readonly CategoryService _categoryService;

        public RssCatListViewComponent(
            ILogger<RssCatListViewComponent> logger,
            IOptions<AppSettings> settings, CategoryService categoryService) : base(logger, settings)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var cats = await _categoryService.GetAllCategoriesAsync();
                var items = cats.Item.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.Title)).ToList();
                var viewModel = new SubscriptionViewModel
                {
                    Cats = items
                };

                return View(viewModel);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error.");

                // should not block website
                return View(new SubscriptionViewModel());
            }
        }
    }
}
