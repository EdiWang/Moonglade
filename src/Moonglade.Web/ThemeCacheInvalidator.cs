namespace Moonglade.Web;

public class ThemeCacheInvalidator
{
    private long _version;

    public string CacheKey => $"{Volatile.Read(ref _version)}:theme";

    public string GetCacheKey(int themeId) => $"{CacheKey}:{themeId}";

    public void Invalidate() => Interlocked.Increment(ref _version);
}
