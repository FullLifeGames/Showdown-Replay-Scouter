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
        private readonly ConcurrentDictionary<string, CacheEntry> _internalCache = new();

        public IDistributedCache? Cache { get; }

        public CacheCollector(IDistributedCache? cache)
        {
            Cache = cache;
        }

        public void SetString(string key, string value)
        {
            Set(key, Encoding.UTF8.GetBytes(value), new DistributedCacheEntryOptions());
        }

        public string? GetString(string key)
        {
            var value = Get(key);
            return value != null ? Encoding.UTF8.GetString(value) : null;
        }

        public void Store()
        {
            foreach (var pairs in _internalCache)
            {
                Cache?.Set(pairs.Key, pairs.Value.Value, pairs.Value.Options);
            }
            _internalCache.Clear();
        }

        public byte[]? Get(string key)
        {
            if (_internalCache.TryGetValue(key, out var value) && value is not null)
            {
                return value.Value;
            }

            return Cache?.Get(key);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _internalCache.AddOrUpdate(
                key,
                new CacheEntry(value, options),
                (_, _) => new CacheEntry(value, options)
            );
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default
        )
        {
            Set(key, value, options);
            return Task.CompletedTask;
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
            _internalCache.TryRemove(key, out _);
            Cache?.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            _internalCache.TryRemove(key, out _);
            if (Cache == null)
            {
                return Task.CompletedTask;
            }
            return Cache.RemoveAsync(key, token);
        }

        private sealed record CacheEntry(byte[] Value, DistributedCacheEntryOptions Options);
    }
}
