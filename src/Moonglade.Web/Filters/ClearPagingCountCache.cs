using Microsoft.AspNetCore.Mvc.Filters;
using Moonglade.Caching;

namespace Moonglade.Web.Filters
{
    public class ClearPagingCountCache : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        public ClearPagingCountCache(IBlogCache cache)
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
}