using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Moonglade.Caching
{
    public interface IBlogCache
    {
        TItem GetOrCreate<TItem>(CacheDivision division, string key, Func<ICacheEntry, TItem> factory);
        Task<TItem> GetOrCreateAsync<TItem>(CacheDivision division, string key, Func<ICacheEntry, Task<TItem>> factory);
        void RemoveAllCache();
        void Remove(CacheDivision division);
        void Remove(CacheDivision division, string key);
    }
}