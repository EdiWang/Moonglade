using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Web.Filters;

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
    private readonly ICacheAside _cache;

    private readonly string _cacheKey;
    private readonly BlogCachePartition _partition;
    private readonly BlogCacheType _type = BlogCacheType.None;

    public ClearBlogCache(BlogCacheType type, ICacheAside cache)
    {
        _cache = cache;
        _type = type;
    }

    public ClearBlogCache(BlogCachePartition partition, string cacheKey, ICacheAside cache)
    {
        _partition = partition;
        _cacheKey = cacheKey;
        _cache = cache;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        if (_type.HasFlag(BlogCacheType.None))
        {
            _cache.Remove(_partition.ToString(), _cacheKey);
        }

        if (_type.HasFlag(BlogCacheType.Subscription))
        {
            _cache.Remove(BlogCachePartition.General.ToString(), "rss");
            _cache.Remove(BlogCachePartition.General.ToString(), "atom");
            _cache.Remove(BlogCachePartition.RssCategory.ToString());
        }

        if (_type.HasFlag(BlogCacheType.SiteMap))
        {
            _cache.Remove(BlogCachePartition.General.ToString(), "sitemap");
        }

        if (_type.HasFlag(BlogCacheType.PagingCount))
        {
            _cache.Remove(BlogCachePartition.General.ToString(), "postcount");
            _cache.Remove(BlogCachePartition.PostCountCategory.ToString());
            _cache.Remove(BlogCachePartition.PostCountTag.ToString());
        }
    }
}