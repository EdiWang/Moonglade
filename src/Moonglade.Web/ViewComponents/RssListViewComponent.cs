using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core.CategoryFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.ViewComponents
{
    public class RssListViewComponent : ViewComponent
    {
        private readonly ILogger<RssListViewComponent> _logger;
        private readonly IMediator _mediator;


        public RssListViewComponent(ILogger<RssListViewComponent> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var cats = await _mediator.Send(new GetCategoriesQuery());
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
