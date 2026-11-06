using System.Security.Cryptography;
using System.Text;

namespace Moonglade.Web;

public class PostCacheInvalidator(ICacheAside cache)
{
    private long _version;
    private long _feedVersion;
    private long _siteMapVersion;

    public string GetCacheKey(string key) => $"{Volatile.Read(ref _version)}:{key}";
    public string GetFeedCacheKey(string key) => $"{Volatile.Read(ref _feedVersion)}:{key}";
    public string GetFeedCacheKey(string key, BlogSettingsSnapshot settings) =>
        $"{GetFeedCacheKey(key)}:{Fingerprint(settings.GeneralSettings.ToJson() + settings.FeedSettings.ToJson() + settings.ImageSettings.ToJson())}";

    public string GetSiteMapCacheKey(BlogSettingsSnapshot settings) =>
        $"{SiteMapCacheKey}:{Fingerprint(settings.GeneralSettings.ToJson())}";

    private static string Fingerprint(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public string SiteMapCacheKey => $"{Volatile.Read(ref _siteMapVersion)}:sitemap";

    public void Invalidate()
    {
        // Old in-flight queries may finish, but their versioned entries are no longer read.
        Interlocked.Increment(ref _version);
        cache.Remove(BlogCachePartition.Post.ToString());
        InvalidateFeeds();
        InvalidateSiteMap();
    }

    public void InvalidateFeeds()
    {
        var previousVersion = Interlocked.Increment(ref _feedVersion) - 1;
        cache.Remove(BlogCachePartition.General.ToString(), $"{previousVersion}:rss");
        cache.Remove(BlogCachePartition.General.ToString(), $"{previousVersion}:atom");
        cache.Remove(BlogCachePartition.RssCategory.ToString());
        cache.Remove(BlogCachePartition.AtomCategory.ToString());
    }

    public void InvalidateSiteMap()
    {
        var previousVersion = Interlocked.Increment(ref _siteMapVersion) - 1;
        cache.Remove(BlogCachePartition.General.ToString(), $"{previousVersion}:sitemap");
    }
}
