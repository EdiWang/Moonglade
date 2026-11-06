namespace Moonglade.Web;

public class ThemeCacheInvalidator(ICacheAside cache)
{
    private long _version;

    public string CacheKey => $"{Volatile.Read(ref _version)}:theme";

    public string GetCacheKey(int themeId) => $"{CacheKey}:{themeId}";

    public void Invalidate()
    {
        var previousVersion = Interlocked.Increment(ref _version) - 1;
        cache.Remove(BlogCachePartition.General.ToString(), $"{previousVersion}:theme");
    }
}
