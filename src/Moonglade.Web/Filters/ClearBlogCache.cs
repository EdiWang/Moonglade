using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Web.Filters;

[Flags]
public enum BlogCacheType
{
    None = 0,
    SiteMap = 4
}

public class ClearBlogCache(BlogCachePartition partition, string cacheKey, ICacheAside cache, PostCacheInvalidator postCacheInvalidator)
    : ActionFilterAttribute
{
    private readonly BlogCacheType _type = BlogCacheType.None;

    public ClearBlogCache(BlogCacheType type, ICacheAside cache, PostCacheInvalidator postCacheInvalidator)
        : this(BlogCachePartition.General, null, cache, postCacheInvalidator)
    {
        _type = type;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        if (_type == BlogCacheType.None)
        {
            cache.Remove(partition.ToString(), cacheKey);
        }

        if (_type.HasFlag(BlogCacheType.SiteMap))
        {
            postCacheInvalidator.InvalidateSiteMap();
        }
    }
}
