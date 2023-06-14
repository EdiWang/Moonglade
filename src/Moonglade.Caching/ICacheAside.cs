using Microsoft.Extensions.Caching.Memory;

namespace Edi.CacheAside.InMemory;

public interface ICacheAside
{
    TItem GetOrCreate<TItem>(string partition, string key, Func<ICacheEntry, TItem> factory);
    Task<TItem> GetOrCreateAsync<TItem>(string partition, string key, Func<ICacheEntry, Task<TItem>> factory);
    void Clear();
    void Remove(string partition);
    void Remove(string partition, string key);
}