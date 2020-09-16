using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace Moonglade.Web.Filters
{
    public class DeleteMemoryCache : ActionFilterAttribute
    {
        private readonly IMemoryCache _memoryCache;

        private readonly string[] _cacheKeys;

        public DeleteMemoryCache(string[] cacheKeys, IMemoryCache memoryCache)
        {
            _cacheKeys = cacheKeys;
            _memoryCache = memoryCache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            foreach (string cacheKey in _cacheKeys)
            {
                _memoryCache.Remove(cacheKey);
            }
        }
    }
}
