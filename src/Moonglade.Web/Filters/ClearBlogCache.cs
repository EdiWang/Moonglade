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

public class ClearBlogCache(BlogCachePartition partition, string cacheKey, ICacheAside cache)
    : ActionFilterAttribute
{
    private readonly BlogCacheType _type = BlogCacheType.None;

    public ClearBlogCache(BlogCacheType type, ICacheAside cache) : this(BlogCachePartition.General, null, cache)
    {
        _type = type;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        if (_type.HasFlag(BlogCacheType.None))
        {
            cache.Remove(partition.ToString(), cacheKey);
        }

        if (_type.HasFlag(BlogCacheType.Subscription))
        {
            cache.Remove(BlogCachePartition.General.ToString(), "rss");
            cache.Remove(BlogCachePartition.General.ToString(), "atom");
            cache.Remove(BlogCachePartition.RssCategory.ToString());
        }

        if (_type.HasFlag(BlogCacheType.SiteMap))
        {
            cache.Remove(BlogCachePartition.General.ToString(), "sitemap");
        }

        if (_type.HasFlag(BlogCacheType.PagingCount))
        {
            cache.Remove(BlogCachePartition.General.ToString(), "postcount");
            cache.Remove(BlogCachePartition.PostCountCategory.ToString());
            cache.Remove(BlogCachePartition.PostCountTag.ToString());
        }
    }
}