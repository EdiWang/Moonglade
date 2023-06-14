using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Edi.CacheAside.InMemory;

public class MemoryCacheAside : ICacheAside
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
    public ConcurrentDictionary<string, IList<string>> CachePartitions { get; }

    private readonly IMemoryCache _memoryCache;

    public MemoryCacheAside(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        CachePartitions = new();
    }

    public TItem GetOrCreate<TItem>(string partition, string key, Func<ICacheEntry, TItem> factory)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        AddToPartition(partition, key);
        return _memoryCache.GetOrCreate($"{partition}-{key}", factory);
    }

    public Task<TItem> GetOrCreateAsync<TItem>(string partition, string key, Func<ICacheEntry, Task<TItem>> factory)
    {
        if (string.IsNullOrWhiteSpace(key)) return Task.FromResult(default(TItem));

        AddToPartition(partition, key);
        return _memoryCache.GetOrCreateAsync($"{partition}-{key}", factory);
    }

    public void Clear()
    {
        var keys =
            from kvp in CachePartitions
            let prefix = kvp.Key
            from val in kvp.Value
            select $"{prefix}-{val}";

        foreach (var key in keys)
        {
            _memoryCache.Remove(key);
        }
    }

    public void Remove(string partition)
    {
        if (!CachePartitions.ContainsKey(partition)) return;

        var cacheKeys = CachePartitions[partition];
        if (cacheKeys.Any())
        {
            foreach (var key in cacheKeys)
            {
                _memoryCache.Remove($"{partition}-{key}");
            }
        }
    }

    public void Remove(string partition, string key)
    {
        if ((string.IsNullOrWhiteSpace(key)) || !CachePartitions.ContainsKey(partition)) return;
        _memoryCache.Remove($"{partition}-{key}");
    }

    private void AddToPartition(string partitionKey, string cacheKey)
    {
        if (!CachePartitions.ContainsKey(partitionKey))
        {
            CachePartitions.TryAdd(partitionKey, new[] { cacheKey }.ToList());
        }

        if (!CachePartitions[partitionKey].Contains(cacheKey))
        {
            CachePartitions[partitionKey].Add(cacheKey);
        }
    }
}