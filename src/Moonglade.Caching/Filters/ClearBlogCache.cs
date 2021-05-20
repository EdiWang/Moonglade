using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Caching.Filters
{
    public class ClearBlogCache : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        private readonly string _cacheKey;
        private readonly CacheDivision _division;

        public ClearBlogCache(CacheDivision division, string cacheKey, IBlogCache cache)
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
