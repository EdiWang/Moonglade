using Microsoft.AspNetCore.Mvc.Filters;
using Moonglade.Core.Caching;

namespace Moonglade.Web.Filters
{
    public class DeleteBlogCacheDivision : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        private readonly string _divisionKey;

        public DeleteBlogCacheDivision(string divisionKey, IBlogCache cache)
        {
            _divisionKey = divisionKey;
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            _cache.Remove(_divisionKey);
        }
    }

    public class DeleteBlogCache : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        private readonly string _cacheKey;
        private readonly string _divisionKey;

        public DeleteBlogCache(string divisionKey, string cacheKey, IBlogCache cache)
        {
            _divisionKey = divisionKey;
            _cacheKey = cacheKey;
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            _cache.Remove(_divisionKey, _cacheKey);
        }
    }
}
