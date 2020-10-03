using System;
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
            try
            {
                var tags = await _tagService.GetHotTagsAsync(_blogConfig.ContentSettings.HotTagAmount);
                return View(tags);
            }
            catch (Exception e)
            {
                ViewBag.ComponentErrorMessage = e.Message;
                return View("~/Views/Shared/ComponentError.cshtml");
            }
        }
    }
}
