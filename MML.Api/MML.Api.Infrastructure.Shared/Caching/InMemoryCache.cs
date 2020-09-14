using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Caching
{
    public class InMemoryCache : ICacheManager
    {
        private Dictionary<string, CacheEntry> Cache;
        public int CacheExpiration { get; set; }

        public InMemoryCache()
        {
            CacheExpiration = 5;
            Cache = new Dictionary<string, CacheEntry>();
        }
        public void Add(string key, object value)
        {
            Cache.Add(key,new CacheEntry {Value = value, Timestamp = DateTime.UtcNow });
        }

        public object Get(string key)
        {
            if (Cache.ContainsKey(key))
            {
                return Cache[key].Value;
            }
            return null;
        }

        public void Remove(string key)
        {
            Cache.Remove(key);
        }

        private void ExpireCache()
        {
            var expiredItems = Cache.Where(m => (DateTime.UtcNow - m.Value.Timestamp).TotalMinutes >= CacheExpiration);
            foreach (var expiredItem in expiredItems)
            {
                Cache.Remove(expiredItem.Key);
            }
        }

        public void ResetExpiration(string key)
        {
            var item =Cache.FirstOrDefault(c=> c.Key == key);
            if(string.IsNullOrEmpty(item.Key))
                throw new KeyNotFoundException(key);
            item.Value.Timestamp = DateTime.UtcNow;
            var temp = DateTime.UtcNow;
            
        }

        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
