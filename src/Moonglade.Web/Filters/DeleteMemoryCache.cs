using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace Moonglade.Web.Filters
{
    public class DeleteMemoryCache : ActionFilterAttribute
    {
        private readonly IMemoryCache _memoryCache;

        private readonly string _cacheKey;

        public DeleteMemoryCache(string cacheKey, IMemoryCache memoryCache)
        {
            _cacheKey = cacheKey;
            _memoryCache = memoryCache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            _memoryCache.Remove(_cacheKey);
        }
    }
}
