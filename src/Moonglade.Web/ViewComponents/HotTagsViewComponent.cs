using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class HotTagsViewComponent : MoongladeViewComponent
    {
        private readonly TagService _tagService;

        private readonly IBlogConfig _blogConfig;

        public HotTagsViewComponent(
            ILogger<HotTagsViewComponent> logger,
            IOptions<AppSettings> settings, 
            TagService tagService, 
            IBlogConfig blogConfig,
            IBlogConfigurationService blogConfigurationService) : base(logger, settings)
        {
            _tagService = tagService;
            _blogConfig = blogConfig;
            _blogConfig.Initialize(blogConfigurationService);
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var response = await _tagService.GetHotTagsAsync(_blogConfig.ContentSettings.HotTagAmount);
            if (response.IsSuccess)
            {
                return View(response.Item);
            }

            ViewBag.ComponentErrorMessage = response.Message;
            return View("~/Views/Shared/ComponentError.cshtml");
        }
    }
}
