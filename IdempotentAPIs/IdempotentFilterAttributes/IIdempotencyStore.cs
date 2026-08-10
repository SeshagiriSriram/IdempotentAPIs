using System;
using System.Collections.Generic;
using System.Text;

namespace IdempotentFilterAttributes
{
    public interface IIdempotencyStore
    {
        Task<bool> TryLockAsync(string key);
        Task<CachedResponse?> GetAsync(string key);
        Task SaveAsync(string key, CachedResponse response);
    }
}
