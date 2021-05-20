using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Caching.Filters
{
    public class ClearSiteMapCache : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        public ClearSiteMapCache(IBlogCache cache)
        {
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            _cache.Remove(CacheDivision.General, "sitemap");
        }
    }
}
