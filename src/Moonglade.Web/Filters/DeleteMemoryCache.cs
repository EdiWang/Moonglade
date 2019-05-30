using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Filters
{
    public class DeleteMemoryCache : ActionFilterAttribute
    {
        private readonly IMemoryCache _memoryCache;

        private string _cacheKey;

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
