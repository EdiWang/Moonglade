using Microsoft.Extensions.Caching.Memory;

namespace Moonglade.CacheAside.InMemory;

public interface ICacheAside
{
    TItem GetOrCreate<TItem>(CachePartition partition, string key, Func<ICacheEntry, TItem> factory);
    Task<TItem> GetOrCreateAsync<TItem>(CachePartition partition, string key, Func<ICacheEntry, Task<TItem>> factory);
    void RemoveAllCache();
    void Remove(CachePartition partition);
    void Remove(CachePartition partition, string key);
}