namespace IdempotentFilterAttributes.Models
{
    public class IdempotencyOptions
    {
        public string HeaderName { get; set; } = "X-Idempotency-Key";
        public int CacheDurationInMinutes { get; set; } = 60;

        // Added Redis connection string option
        public string RedisConnectionString { get; set; } = "localhost:6379";
    }
}
