using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class HotTagsViewComponent : ViewComponent
    {
        private readonly TagService _tagService;

        private readonly IBlogConfig _blogConfig;

        public HotTagsViewComponent(TagService tagService, IBlogConfig blogConfig)
        {
            _tagService = tagService;
            _blogConfig = blogConfig;
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
