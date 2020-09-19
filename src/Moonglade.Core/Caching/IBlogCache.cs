using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Moonglade.Core.Caching
{
    public interface IBlogCache
    {
        Dictionary<string, IList<string>> CacheDivision { get; }
        TItem GetOrCreate<TItem>(string divisionKey, string key, Func<ICacheEntry, TItem> factory);
        Task<TItem> GetOrCreateAsync<TItem>(string divisionKey, string key, Func<ICacheEntry, Task<TItem>> factory);
        void RemoveAllCache();
        void Remove(string divisionKey);
        void Remove(string divisionKey, string key);
    }
}