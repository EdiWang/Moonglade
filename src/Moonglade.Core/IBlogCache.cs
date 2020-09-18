using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Moonglade.Core
{
    public interface IBlogCache
    {
        Dictionary<string, IList<string>> CacheDivision { get; }
        TItem GetOrCreate<TItem>(string divisionKey, string key, Func<ICacheEntry, TItem> factory);
        Task<TItem> GetOrCreateAsync<TItem>(string divisionKey, string key, Func<ICacheEntry, Task<TItem>> factory);
        void AddToDivision(string divisionKey, IEnumerable<string> cacheKeys = null);
        string AddToDivision(string divisionKey, string cacheKey);
        void RemoveAllCache();
        void RemoveCache(string divisionKey);
        void Remove(string key);
    }
}