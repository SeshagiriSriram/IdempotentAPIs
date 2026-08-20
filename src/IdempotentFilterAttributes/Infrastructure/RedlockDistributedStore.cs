using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IdempotentFilterAttributes.Core;
using IdempotentFilterAttributes.Models;
using Microsoft.Extensions.Logging; // Added for diagnostic logging
using StackExchange.Redis;

namespace IdempotentFilterAttributes.Infrastructure
{
    public class RedlockDistributedStore : IIdempotencyStore
    {
        private readonly List<IDatabase> _databases;
        private readonly ILogger<RedlockDistributedStore> _logger; // Logger instance

        // StackExchange.Redis Lua parameters use explicit '@parameterName' keys matching standard property objects
        private static readonly LuaScript AcquireScript = LuaScript.Prepare(@"
            if redis.call('exists', @key) == 0 then
                redis.call('set', @key, @token, 'EX', @ttl)
                return 1
            else
                return 0
            end");

        private static readonly LuaScript ReleaseScript = LuaScript.Prepare(@"
            if redis.call('get', @key) == @token then
                return redis.call('del', @key)
            else
                return 0
            end");

        // Injecting the ILogger cleanly into the Store layer
        public RedlockDistributedStore(
            IEnumerable<IConnectionMultiplexer> multiplexers,
            ILogger<RedlockDistributedStore> logger)
        {
            _databases = multiplexers.Select(m => m.GetDatabase()).ToList();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("Redlock Distributed Store initialized with {Count} independent Redis node instances.", _databases.Count);
        }

        public async Task<bool> TryLockAsync(string key)
        {
            string lockKey = $"lock:{key}";
            string token = Guid.NewGuid().ToString("N");
            int ttlSeconds = 60; // 60-second safety fallback window

            int majority = (_databases.Count / 2) + 1;
            int successfulWrites = 0;

            _logger.LogInformation("Attempting distributed lock for Key: {Key}. (Targeting Majority Quorum of {Majority} nodes)", lockKey, majority);

            var tasks = _databases.Select(async (db, index) =>
            {
                try
                {
                    // Using Named parameters (@key, @token, @ttl) matching the anonymous runtime object properties
                    var result = await db.ScriptEvaluateAsync(AcquireScript, new
                    {
                        key = (RedisKey)lockKey,
                        token = (RedisValue)token,
                        ttl = ttlSeconds
                    });

                    bool nodeLocked = (int)result == 1;
                    _logger.LogDebug("Redis Node [{Index}] execution status: {Status}", index, nodeLocked ? "Lock Granted" : "Lock Denied/Held");
                    return nodeLocked;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Connection Error or Exception hit on Redis Node [{Index}] during TryLockAsync", index);
                    return false;
                }
            });

            var results = await Task.WhenAll(tasks);
            successfulWrites = results.Count(success => success);

            _logger.LogInformation("Lock attempt completed. Successful Node Writes: {Count}/{Total} nodes.", successfulWrites, _databases.Count);

            if (successfulWrites >= majority)
            {
                _logger.LogInformation("✅ Distributed Lock consensus successfully achieved for key: {Key}", lockKey);
                return true;
            }

            _logger.LogWarning("🚩Consensus Failed. Rolling back partial lock modifications for key: {Key}", lockKey);
            await ReleaseDistributedLockAsync(lockKey, token);
            return false;
        }

        public async Task ReleaseLockAsync(string key)
        {
            string lockKey = $"lock:{key}";
            _logger.LogInformation("Filter triggered ReleaseLockAsync pipeline for: {Key}. Clearing keys broadly.", lockKey);

            var tasks = _databases.Select(async (db, index) =>
            {
                try
                {
                    await db.KeyDeleteAsync(lockKey);
                    _logger.LogDebug("Node [{Index}] manually cleared for key: {Key}", index, lockKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while executing forced key drop on Node [{Index}]", index);
                }
            });
            await Task.WhenAll(tasks);
        }

        private async Task ReleaseDistributedLockAsync(string lockKey, string token)
        {
            var tasks = _databases.Select(async (db, index) =>
            {
                try
                {
                    await db.ScriptEvaluateAsync(ReleaseScript, new
                    {
                        key = (RedisKey)lockKey,
                        token = (RedisValue)token
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while performing standard signature validation unlock on Node [{Index}]", index);
                }
            });
            await Task.WhenAll(tasks);
        }

        public async Task<CachedResponse?> GetAsync(string key)
        {
            string cacheKey = $"response:{key}";
            _logger.LogInformation("Querying all distributed cluster nodes simultaneously for key: {Key}", cacheKey);

            // FIX: Launch connection tasks to ALL nodes in parallel at the exact same time
            var tasks = _databases.Select(async (db, index) =>
            {
                try
                {
                    var cachedData = await db.StringGetAsync(cacheKey);
                    if (cachedData.HasValue)
                    {
                        return cachedData.ToString();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("🚩 Bypassing unresponsive Redis Node [{Index}] while fetching cache entry.", index);
                    _logger.LogWarning("🚩 {0}", ex.Message); 
                }
                return null;
            });

            // Wait for all nodes to respond or timeout concurrently
            var results = await Task.WhenAll(tasks);

            // Pick the first non-null string payload returned by any surviving node
            var validJson = results.FirstOrDefault(json => json != null);

            if (validJson != null)
            {
                _logger.LogInformation("🎯 Parallel Cache Hit detected for Key: {Key}", cacheKey);
                return JsonSerializer.Deserialize<CachedResponse>(validJson);
            }

            _logger.LogInformation("Cache Miss: No payload data exists in active nodes for key: {Key}", cacheKey);
            return null;
        }

        public async Task SaveAsync(string key, CachedResponse response, int cacheDurationInMinutes)
        {
            string cacheKey = $"response:{key}";
            string serializedData = JsonSerializer.Serialize(response);
            _logger.LogInformation("Broadcasting and caching final output payload to cluster nodes for key: {Key} (TTL: {Minutes}m)", cacheKey, cacheDurationInMinutes);

            var tasks = _databases.Select(async (db, index) =>
            {
                try
                {
                    await db.StringSetAsync(cacheKey, serializedData, TimeSpan.FromMinutes(cacheDurationInMinutes));
                    _logger.LogDebug("Payload written successfully to Node [{Index}]", index);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write payload broadcast out to Node [{Index}]", index);
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
