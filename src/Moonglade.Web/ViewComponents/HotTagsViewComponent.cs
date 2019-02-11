using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class HotTagsViewComponent : MoongladeViewComponent
    {
        private readonly TagService _tagService;

        public HotTagsViewComponent(
            ILogger<HotTagsViewComponent> logger,
            MoongladeDbContext context,
            IOptions<AppSettings> settings, TagService tagService) : base(logger, context, settings)
        {
            _tagService = tagService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            await Task.CompletedTask;
            var response = _tagService.GetHotTags(AppSettings.HotTagAmount);
            if (response.IsSuccess)
            {
                return View(response.Item);
            }
            // should not block website
            return View(new List<TagInfo>());
        }
    }
}
