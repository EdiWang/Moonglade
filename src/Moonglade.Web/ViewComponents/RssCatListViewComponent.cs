using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Web.Models;

namespace Moonglade.Web.ViewComponents
{
    public class RssCatListViewComponent : MoongladeViewComponent
    {
        private readonly CategoryService _categoryService;

        public RssCatListViewComponent(
            ILogger<RssCatListViewComponent> logger, CategoryService categoryService) : base(logger)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var cats = await _categoryService.GetAllCategoriesAsync();
                var items = cats.Item.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.Name)).ToList();
                var viewModel = new SubscriptionViewModel
                {
                    Cats = items
                };

                return View(viewModel);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error.");

                ViewBag.ComponentErrorMessage = e.Message;
                return View("~/Views/Shared/ComponentError.cshtml");
            }
        }
    }
}
