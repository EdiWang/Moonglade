using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Core.PostFeature;
using Moonglade.Core.TagFeature;
using System.Threading.Tasks;
using X.PagedList;

namespace Moonglade.Web.Pages
{
    public class TagListModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly IBlogConfig _blogConfig;
        private readonly IPostQueryService _postQueryService;
        private readonly IBlogCache _cache;

        [BindProperty(SupportsGet = true)]
        public int P { get; set; }
        public StaticPagedList<PostDigest> Posts { get; set; }

        public TagListModel(IMediator mediator, IBlogConfig blogConfig, IPostQueryService postQueryService, IBlogCache cache)
        {
            _mediator = mediator;
            _blogConfig = blogConfig;
            _postQueryService = postQueryService;
            _cache = cache;

            P = 1;
        }

        public async Task<IActionResult> OnGet(string normalizedName)
        {
            var tagResponse = await _mediator.Send(new GetTagQuery(normalizedName));
            if (tagResponse is null) return NotFound();

            var pagesize = _blogConfig.ContentSettings.PostListPageSize;
            var posts = await _mediator.Send(new ListByTagQuery(tagResponse.Id, pagesize, P));
            var count = _cache.GetOrCreate(CacheDivision.PostCountTag, tagResponse.Id.ToString(), _ => _postQueryService.CountByTag(tagResponse.Id));

            ViewData["TitlePrefix"] = tagResponse.DisplayName;

            var list = new StaticPagedList<PostDigest>(posts, P, pagesize, count);
            Posts = list;

            return Page();
        }
    }
}
