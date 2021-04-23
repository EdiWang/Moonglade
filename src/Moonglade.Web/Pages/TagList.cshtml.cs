using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core;
using X.PagedList;

namespace Moonglade.Web.Pages
{
    public class TagListModel : PageModel
    {
        private readonly ITagService _tagService;
        private readonly IBlogConfig _blogConfig;
        private readonly IPostQueryService _postQueryService;
        private readonly IBlogCache _cache;

        [BindProperty(SupportsGet = true)]
        public int P { get; set; }

        public TagListModel(
            ITagService tagService, IBlogConfig blogConfig, IPostQueryService postQueryService, IBlogCache cache)
        {
            _tagService = tagService;
            _blogConfig = blogConfig;
            _postQueryService = postQueryService;
            _cache = cache;

            P = 1;
        }

        public StaticPagedList<PostDigest> Posts { get; set; }

        public async Task<IActionResult> OnGet(string normalizedName)
        {
            var tagResponse = _tagService.Get(normalizedName);
            if (tagResponse is null) return NotFound();

            var pagesize = _blogConfig.ContentSettings.PostListPageSize;
            var posts = await _postQueryService.ListByTag(tagResponse.Id, pagesize, P);
            var count = _cache.GetOrCreate(CacheDivision.PostCountTag, tagResponse.Id.ToString(), _ => _postQueryService.CountByTag(tagResponse.Id));

            ViewData["TitlePrefix"] = tagResponse.DisplayName;

            var list = new StaticPagedList<PostDigest>(posts, P, pagesize, count);
            Posts = list;

            return Page();
        }
    }
}
