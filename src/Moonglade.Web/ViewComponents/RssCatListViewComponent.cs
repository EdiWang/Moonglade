using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;

namespace Moonglade.Web.ViewComponents
{
    public class RssCatListViewComponent : MoongladeViewComponent
    {
        public RssCatListViewComponent(
            ILogger<RssCatListViewComponent> logger,
            MoongladeDbContext context,
            IOptions<AppSettings> settings) : base(logger, context, settings)
        {
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                await Task.CompletedTask;
                var query = Context.Category.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.Title));
                var viewModel = new SubscriptionViewModel
                {
                    cats = query.ToList()
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
