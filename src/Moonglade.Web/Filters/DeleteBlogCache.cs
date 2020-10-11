using Microsoft.AspNetCore.Mvc.Filters;
using Moonglade.Caching;

namespace Moonglade.Web.Filters
{
    public class DeleteBlogCacheDivision : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        private readonly CacheDivision _division;

        public DeleteBlogCacheDivision(CacheDivision division, IBlogCache cache)
        {
            _division = division;
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            _cache.Remove(_division);
        }
    }

    public class DeleteBlogCache : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        private readonly string _cacheKey;
        private readonly CacheDivision _division;

        public DeleteBlogCache(CacheDivision division, string cacheKey, IBlogCache cache)
        {
            _division = division;
            _cacheKey = cacheKey;
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            _cache.Remove(_division, _cacheKey);
        }
    }
}
