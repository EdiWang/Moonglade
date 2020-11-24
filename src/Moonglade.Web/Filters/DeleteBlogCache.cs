using Microsoft.AspNetCore.Mvc.Filters;
using Moonglade.Caching;

namespace Moonglade.Web.Filters
{
    public class DeletePagingCountCache : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        public DeletePagingCountCache(IBlogCache cache)
        {
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            _cache.Remove(CacheDivision.General, "postcount");
            _cache.Remove(CacheDivision.PostCountCategory);
            _cache.Remove(CacheDivision.PostCountTag);
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
