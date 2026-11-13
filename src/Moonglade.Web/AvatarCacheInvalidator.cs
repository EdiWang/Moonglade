namespace Moonglade.Web;

public sealed class AvatarCacheInvalidator(ICacheAside cache)
{
    private long _version;

    public string CacheKey => GetCacheKey(Volatile.Read(ref _version));

    public void Invalidate()
    {
        var previousVersion = Interlocked.Increment(ref _version) - 1;
        cache.Remove(nameof(BlogCachePartition.Account), GetCacheKey(previousVersion));
    }

    private static string GetCacheKey(long version) => $"{version}:avatar";
}
