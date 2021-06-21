using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Caching.Filters
{
    public enum BlogCacheType
    {
        None,
        Subscription,
        SiteMap,
        PagingCount
    }

    public class ClearBlogCache : ActionFilterAttribute
    {
        private readonly IBlogCache _cache;

        private readonly string _cacheKey;
        private readonly CacheDivision _division;
        private readonly BlogCacheType _type = BlogCacheType.None;

        public ClearBlogCache(BlogCacheType type, IBlogCache cache)
        {
            _cache = cache;
            _type = type;
        }

        public ClearBlogCache(CacheDivision division, string cacheKey, IBlogCache cache)
        {
            _division = division;
            _cacheKey = cacheKey;
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            switch (_type)
            {
                case BlogCacheType.None:
                    _cache.Remove(_division, _cacheKey);
                    break;
                case BlogCacheType.Subscription:
                    _cache.Remove(CacheDivision.General, "rss");
                    _cache.Remove(CacheDivision.General, "atom");
                    _cache.Remove(CacheDivision.RssCategory);
                    break;
                case BlogCacheType.SiteMap:
                    _cache.Remove(CacheDivision.General, "sitemap");
                    break;
                case BlogCacheType.PagingCount:
                    _cache.Remove(CacheDivision.General, "postcount");
                    _cache.Remove(CacheDivision.PostCountCategory);
                    _cache.Remove(CacheDivision.PostCountTag);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
