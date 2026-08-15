using System;
using System.Text.Json;
using System.Threading.Tasks;
using IdempotentFilterAttributes.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace IdempotentFilterAttributes
{
    public class RedisIdempotencyStore : IIdempotencyStore
    {
        private readonly IDatabase _db;

        public RedisIdempotencyStore(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
            
        }

        public async Task<bool> TryLockAsync(string key)
        {
            string lockKey = $"lock:{key}";

            // Phase 1 Lock: Set the key ONLY if it does not already exist (NX flag)
            // Sets a 2-minute safety expiry so a crashed node doesn't leave it locked forever
            return await _db.StringSetAsync(
                lockKey,
                "locked",
                expiry: TimeSpan.FromMinutes(2),
                when: When.NotExists);
        }

        public async Task ReleaseLockAsync(string key)
        {
            string lockKey = $"lock:{key}";
            await _db.KeyDeleteAsync(lockKey);
        }

        public async Task<CachedResponse?> GetAsync(string key)
        {
            string cacheKey = $"response:{key}";
            var cachedData = await _db.StringGetAsync(cacheKey);

            if (!cachedData.HasValue)
            {
                return null;
            }

            return JsonSerializer.Deserialize<CachedResponse>(cachedData!.ToString());
        }

        public async Task SaveAsync(string key, CachedResponse response, int cacheDurationInMinutes)
        {
            string cacheKey = $"response:{key}";
            string serializedData = JsonSerializer.Serialize(response);

            // Save response with the configured absolute expiration TTL
            await _db.StringSetAsync(
                cacheKey,
                serializedData,
                expiry: TimeSpan.FromMinutes(cacheDurationInMinutes));
        }
    }
}
