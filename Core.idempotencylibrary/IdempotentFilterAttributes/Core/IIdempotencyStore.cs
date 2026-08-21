using IdempotentFilterAttributes.Models;
namespace IdempotentFilterAttributes.Core
{
    public interface IIdempotencyStore
    {
        // Concurrency protection: returns true if lock acquired successfully
        Task<bool> TryLockAsync(string key);

        // Concurrency release: frees the lock for incoming retries
        Task ReleaseLockAsync(string key);

        // Retrieves cached responses
        Task<CachedResponse?> GetAsync(string key);

        // Saves responses with the resolved time-to-live (TTL) duration
        Task SaveAsync(string key, CachedResponse response, int cacheDurationInMinutes);
    }
}

