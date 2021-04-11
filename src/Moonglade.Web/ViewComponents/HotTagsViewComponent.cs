using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Configuration;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class HotTagsViewComponent : ViewComponent
    {
        private readonly ITagService _tagService;

        private readonly IBlogConfig _blogConfig;

        public HotTagsViewComponent(ITagService tagService, IBlogConfig blogConfig)
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
                return Content(e.Message);
            }
        }
    }
}
