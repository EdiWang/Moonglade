namespace Moonglade.Web;

public class PostCacheInvalidator(ICacheAside cache)
{
    private long _version;
    private long _siteMapVersion;

    public string GetCacheKey(string key) => $"{Volatile.Read(ref _version)}:{key}";
    public string SiteMapCacheKey => $"{Volatile.Read(ref _siteMapVersion)}:sitemap";

    public void Invalidate()
    {
        // Old in-flight queries may finish, but their versioned entries are no longer read.
        var previousVersion = Interlocked.Increment(ref _version) - 1;

        cache.Remove(BlogCachePartition.Post.ToString());
        cache.Remove(BlogCachePartition.General.ToString(), $"{previousVersion}:rss");
        cache.Remove(BlogCachePartition.General.ToString(), $"{previousVersion}:atom");
        cache.Remove(BlogCachePartition.RssCategory.ToString());
        cache.Remove(BlogCachePartition.AtomCategory.ToString());
        InvalidateSiteMap();
    }

    public void InvalidateSiteMap()
    {
        var previousVersion = Interlocked.Increment(ref _siteMapVersion) - 1;
        cache.Remove(BlogCachePartition.General.ToString(), $"{previousVersion}:sitemap");
    }
}
