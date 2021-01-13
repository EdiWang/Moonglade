using Microsoft.AspNetCore.Mvc.Filters;
using Moonglade.Caching;

namespace Moonglade.Web.Filters
{
    public class ClearSubscriptionCache : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        public ClearSubscriptionCache(IBlogCache cache)
        {
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            _cache.Remove(CacheDivision.General, "rss");
            _cache.Remove(CacheDivision.General, "atom");
            _cache.Remove(CacheDivision.RssCategory);
        }
    }
}
