using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core;
using X.PagedList;

namespace Moonglade.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        private readonly IPostQueryService _postQueryService;
        private readonly IBlogCache _cache;

        public StaticPagedList<PostDigest> Posts { get; set; }

        public IndexModel(
            IBlogConfig blogConfig, IPostQueryService postQueryService, IBlogCache cache)
        {
            _blogConfig = blogConfig;
            _postQueryService = postQueryService;
            _cache = cache;
        }

        public async Task OnGet(int p = 1)
        {
            var pagesize = _blogConfig.ContentSettings.PostListPageSize;
            var posts = await _postQueryService.ListAsync(pagesize, p);
            var count = _cache.GetOrCreate(CacheDivision.General, "postcount", _ => _postQueryService.CountPublic());

            var list = new StaticPagedList<PostDigest>(posts, p, pagesize, count);

            Posts = list;
        }
    }
}
