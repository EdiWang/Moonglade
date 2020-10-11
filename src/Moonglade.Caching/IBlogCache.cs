using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Moonglade.Caching
{
    public interface IBlogCache
    {
        ConcurrentDictionary<string, IList<string>> CacheDivision { get; }
        TItem GetOrCreate<TItem>(CacheDivision division, string key, Func<ICacheEntry, TItem> factory);
        Task<TItem> GetOrCreateAsync<TItem>(CacheDivision division, string key, Func<ICacheEntry, Task<TItem>> factory);
        void RemoveAllCache();
        void Remove(CacheDivision division);
        void Remove(CacheDivision division, string key);
    }
}