using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace ShowdownReplayScouter.Core.Util
{
    /// <summary>
    /// This class overwrites the SetString and GetString method and provides a Store method to store all cache entries at once
    /// </summary>
    public class CacheCollector : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, string> _internalCache = new();

        public IDistributedCache? Cache { get; }

        public CacheCollector(IDistributedCache? cache)
        {
            Cache = cache;
        }

        public void SetString(string key, string value)
        {
            _internalCache.TryAdd(key, value);
        }

        public string? GetString(string key)
        {
            var success = _internalCache.TryGetValue(key, out string? value);
            if (success)
            {
                return value;
            }
            else
            {
                return Cache?.GetString(key);
            }
        }

        public void Store()
        {
            foreach (var pairs in _internalCache)
            {
                Cache?.SetString(pairs.Key, pairs.Value);
            }
            _internalCache.Clear();
        }

        public byte[]? Get(string key)
        {
            var str = GetString(key);
            return str != null ? Encoding.UTF8.GetBytes(str) : null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.Run(() => Get(key), token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Cache?.Set(key, value, options);
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default
        )
        {
            if (Cache == null)
            {
                return Task.CompletedTask;
            }
            return Cache.SetAsync(key, value, options, token);
        }

        public void Refresh(string key)
        {
            Cache?.Refresh(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            if (Cache == null)
            {
                return Task.CompletedTask;
            }
            return Cache.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            Cache?.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (Cache == null)
            {
                return Task.CompletedTask;
            }
            return Cache.RemoveAsync(key, token);
        }
    }
}
