using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace IdempotentFilterAttributes
{
    public class InMemoryIdempotencyStore: IIdempotencyStore
    {
        private readonly ConcurrentDictionary<string, CachedResponse> _cache = new();
        private readonly ConcurrentDictionary<string, bool> _locks = new();
        public Task<bool> TryLockAsync(string key) =>
        Task.FromResult(_locks.TryAdd(key, true));

        public Task<CachedResponse?> GetAsync(string key) =>
            Task.FromResult(_cache.TryGetValue(key, out var response) ? response : null);

        public Task SaveAsync(string key, CachedResponse response)
        {
            _cache[key] = response;
            _locks.TryRemove(key, out _); // Release lock after saving
            return Task.CompletedTask;
        }
    }
}
