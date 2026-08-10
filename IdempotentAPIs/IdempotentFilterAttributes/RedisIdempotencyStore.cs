using StackExchange.Redis;
using System.Text.Json;

namespace IdempotentFilterAttributes
{
    public class RedisIdempotencyStore:IIdempotencyStore
    {
        private readonly IDatabase _db;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);
        private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(2);

        public RedisIdempotencyStore(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<bool> TryLockAsync(string key) =>
            await _db.StringSetAsync($"lock:{key}", "1", LockExpiry, When.NotExists);

        public async Task<CachedResponse?> GetAsync(string key)
        {
            var data = await _db.StringGetAsync($"cache:{key}");
            return data.HasValue ? JsonSerializer.Deserialize<CachedResponse>(data!.ToString()) : null;
        }

        public async Task SaveAsync(string key, CachedResponse response)
        {
            var json = JsonSerializer.Serialize(response);

            // Save cache and remove lock atomically
            var batch = _db.CreateTransaction();
            _ = batch.StringSetAsync($"cache:{key}", json, CacheExpiry);
            _ = batch.KeyDeleteAsync($"lock:{key}");
            await batch.ExecuteAsync();
        }

    }
}
