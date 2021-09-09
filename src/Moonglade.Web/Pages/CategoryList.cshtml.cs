using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core.CategoryFeature;
using Moonglade.Core.PostFeature;
using System.Threading.Tasks;
using X.PagedList;

namespace Moonglade.Web.Pages
{
    public class CategoryListModel : PageModel
    {
        private readonly IMediator _mediator;
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogCache _cache;

        [BindProperty(SupportsGet = true)]
        public int P { get; set; }
        public StaticPagedList<PostDigest> Posts { get; set; }

        public CategoryListModel(
            IBlogConfig blogConfig,
            IMediator mediator,
            IBlogCache cache)
        {
            _blogConfig = blogConfig;
            _mediator = mediator;
            _cache = cache;

            P = 1;
        }

        public async Task<IActionResult> OnGetAsync(string routeName)
        {
            if (string.IsNullOrWhiteSpace(routeName)) return NotFound();

            var pageSize = _blogConfig.ContentSettings.PostListPageSize;
            var cat = await _mediator.Send(new GetCategoryByRouteCommand(routeName));

            if (cat is null) return NotFound();

            ViewData["CategoryDisplayName"] = cat.DisplayName;
            ViewData["CategoryRouteName"] = cat.RouteName;
            ViewData["CategoryDescription"] = cat.Note;

            var postCount = await _cache.GetOrCreateAsync(CacheDivision.PostCountCategory, cat.Id.ToString(),
                _ => _mediator.Send(new CountPostQuery(CountType.Category, cat.Id)));

            var postList = await _mediator.Send(new ListPostsQuery(pageSize, P, cat.Id));

            Posts = new(postList, P, pageSize, postCount);
            return Page();
        }
    }
}
