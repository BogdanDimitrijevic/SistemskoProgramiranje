using System;
using System.Collections.Concurrent;
using System.Linq;



    public static class Cache
    {
        private static readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();

        public static List<Event> GetFromCache(string key)
        {
            if (_cache.TryGetValue(key, out CacheItem cacheItem) && (DateTime.Now - cacheItem.Timestamp) <= TimeSpan.FromMinutes(15))
            {
                return cacheItem.Data;
            }
            else
            {
                _cache.TryRemove(key, out _);
                return null;
            }
        }

        public static void AddToCache(string key, List<Event> data)
        {
            var item = new CacheItem(data);
            _cache.AddOrUpdate(key, item, (k, v) => item);
        }

        public static void CleanupCache()
        {
            var expiredKeys = _cache.Where(kvp => (DateTime.Now - kvp.Value.Timestamp) > TimeSpan.FromMinutes(15))
                                    .Select(kvp => kvp.Key)
                                    .ToList();
            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    public class CacheItem
    {
        public List<Event> Data { get; }
        public DateTime Timestamp { get; }

        public CacheItem(List<Event> data)
        {
            Timestamp = DateTime.Now;
            Data = data;
        }
    }
