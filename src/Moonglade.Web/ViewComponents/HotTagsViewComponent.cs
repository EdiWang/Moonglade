using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class HotTagsViewComponent : MoongladeViewComponent
    {
        private readonly TagService _tagService;

        public HotTagsViewComponent(
            ILogger<HotTagsViewComponent> logger,
            IOptions<AppSettings> settings, TagService tagService) : base(logger, settings)
        {
            _tagService = tagService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var response = await _tagService.GetHotTagsAsync(AppSettings.HotTagAmount);
            return View(response.IsSuccess ? response.Item : new List<TagInfo>());
        }
    }
}
