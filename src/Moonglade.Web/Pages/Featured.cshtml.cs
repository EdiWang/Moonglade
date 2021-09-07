using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Core.PostFeature;
using System.Threading.Tasks;
using X.PagedList;

namespace Moonglade.Web.Pages
{
    public class FeaturedModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        private readonly IPostQueryService _postQueryService;
        private readonly IMediator _mediator;
        private readonly IBlogCache _cache;
        public StaticPagedList<PostDigest> Posts { get; set; }

        public FeaturedModel(
            IBlogConfig blogConfig, IPostQueryService postQueryService, IBlogCache cache, IMediator mediator)
        {
            _blogConfig = blogConfig;
            _postQueryService = postQueryService;
            _cache = cache;
            _mediator = mediator;
        }

        public async Task OnGet(int p = 1)
        {
            var pagesize = _blogConfig.ContentSettings.PostListPageSize;
            var posts = await _mediator.Send(new ListFeaturedQuery(pagesize, p));
            var count = _cache.GetOrCreate(CacheDivision.PostCountFeatured, "featured", _ => _postQueryService.CountByFeatured());

            var list = new StaticPagedList<PostDigest>(posts, p, pagesize, count);
            Posts = list;
        }
    }
}
