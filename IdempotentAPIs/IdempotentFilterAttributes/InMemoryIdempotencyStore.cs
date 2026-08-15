using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory; // Make sure to add this package dependency

namespace IdempotentFilterAttributes
{
    public class InMemoryIdempotencyStore : IIdempotencyStore
    {
        private readonly IMemoryCache _memoryCache;
        private static readonly object LockObject = new();

        public InMemoryIdempotencyStore(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public Task<bool> TryLockAsync(string key)
        {
            string lockKey = $"lock:{key}";

            // Atomically check and set an entry for the lock using IMemoryCache
            lock (LockObject)
            {
                if (_memoryCache.TryGetValue(lockKey, out _))
                {
                    return Task.FromResult(false); // Lock already held
                }

                // Set lock with a short safety timeout (e.g., 2 minutes) so a crashed request never permanently blocks a key
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(2));

                _memoryCache.Set(lockKey, true, cacheEntryOptions);
                return Task.FromResult(true); // Lock acquired
            }
        }

        public Task ReleaseLockAsync(string key)
        {
            string lockKey = $"lock:{key}";
            _memoryCache.Remove(lockKey);
            return Task.CompletedTask;
        }

        public Task<CachedResponse?> GetAsync(string key)
        {
            string cacheKey = $"response:{key}";
            _memoryCache.TryGetValue(cacheKey, out CachedResponse? cachedResponse);
            return Task.FromResult(cachedResponse);
        }

        public Task SaveAsync(string key, CachedResponse response, int cacheDurationInMinutes)
        {
            string cacheKey = $"response:{key}";

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheDurationInMinutes));

            _memoryCache.Set(cacheKey, response, cacheEntryOptions);
            return Task.CompletedTask;
        }
    }


}
