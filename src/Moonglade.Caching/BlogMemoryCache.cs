using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Moonglade.Caching;

public enum CachePartition
{
    General,
    Post,
    Page,
    PostCountCategory,
    PostCountTag,
    PostCountFeatured,
    RssCategory
}

public class BlogMemoryCache : IBlogCache
{
    /* Create Key-Value mapping for cache divisions to workaround
     * https://github.com/aspnet/Caching/issues/422
     * This blog will need cache keys for post ids or category ids and need to be cleared later
     * Key               | Value
     * ------------------+--------------------------------------
     * PostCountCategory | { "<guid>", "<guid>", ... }
     * Post              | { "<guid>", "<guid>", "<guid"> ... }
     * General           | { "avatar", ... }
     */
    public ConcurrentDictionary<string, IList<string>> CacheDivision { get; }

    private readonly IMemoryCache _memoryCache;

    public BlogMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        CacheDivision = new();
    }

    public TItem GetOrCreate<TItem>(CachePartition partition, string key, Func<ICacheEntry, TItem> factory)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        AddToDivision(partition.ToString(), key);
        return _memoryCache.GetOrCreate($"{partition}-{key}", factory);
    }

    public Task<TItem> GetOrCreateAsync<TItem>(CachePartition partition, string key, Func<ICacheEntry, Task<TItem>> factory)
    {
        if (string.IsNullOrWhiteSpace(key)) return Task.FromResult(default(TItem));

        AddToDivision(partition.ToString(), key);
        return _memoryCache.GetOrCreateAsync($"{partition}-{key}", factory);
    }

    public void RemoveAllCache()
    {
        var keys =
            from kvp in CacheDivision
            let prefix = kvp.Key
            from val in kvp.Value
            select $"{prefix}-{val}";

        foreach (var key in keys)
        {
            _memoryCache.Remove(key);
        }
    }

    public void Remove(CachePartition partition)
    {
        if (!CacheDivision.ContainsKey(partition.ToString())) return;

        var cacheKeys = CacheDivision[partition.ToString()];
        if (cacheKeys.Any())
        {
            foreach (var key in cacheKeys)
            {
                _memoryCache.Remove($"{partition}-{key}");
            }
        }
    }

    public void Remove(CachePartition partition, string key)
    {
        if ((string.IsNullOrWhiteSpace(key)) || !CacheDivision.ContainsKey(partition.ToString())) return;
        _memoryCache.Remove($"{partition}-{key}");
    }

    private void AddToDivision(string divisionKey, string cacheKey)
    {
        if (!CacheDivision.ContainsKey(divisionKey))
        {
            CacheDivision.TryAdd(divisionKey, new[] { cacheKey }.ToList());
        }

        if (!CacheDivision[divisionKey].Contains(cacheKey))
        {
            CacheDivision[divisionKey].Add(cacheKey);
        }
    }
}