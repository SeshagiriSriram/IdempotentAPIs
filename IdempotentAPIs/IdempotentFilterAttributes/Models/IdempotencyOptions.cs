using System.Collections.Generic;

namespace IdempotentFilterAttributes.Models
{
    public class IdempotencyOptions
    {
        public string HeaderName { get; set; } = "X-Idempotency-Key";
        public int CacheDurationInMinutes { get; set; } = 60;
        public List<string> RedisNodes { get; set; } = new();
    }
}
