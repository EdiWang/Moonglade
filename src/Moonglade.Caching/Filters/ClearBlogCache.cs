using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Caching.Filters;

[Flags]
public enum BlogCacheType
{
    None = 1,
    Subscription = 2,
    SiteMap = 4,
    PagingCount = 8
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

        if (_type.HasFlag(BlogCacheType.None))
        {
            _cache.Remove(_division, _cacheKey);
        }

        if (_type.HasFlag(BlogCacheType.Subscription))
        {
            _cache.Remove(CacheDivision.General, "rss");
            _cache.Remove(CacheDivision.General, "atom");
            _cache.Remove(CacheDivision.RssCategory);
        }

        if (_type.HasFlag(BlogCacheType.SiteMap))
        {
            _cache.Remove(CacheDivision.General, "sitemap");
        }

        if (_type.HasFlag(BlogCacheType.PagingCount))
        {
            _cache.Remove(CacheDivision.General, "postcount");
            _cache.Remove(CacheDivision.PostCountCategory);
            _cache.Remove(CacheDivision.PostCountTag);
        }
    }
}