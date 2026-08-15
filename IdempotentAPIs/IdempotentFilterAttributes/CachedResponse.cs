namespace IdempotentFilterAttributes
{
    public class CachedResponse
    {
        public int StatusCode { get; set; }

        // Changed from object? to string? to store the raw serialized JSON string safely
        public string? Content { get; set; }

        public string RequestHash { get; set; } = string.Empty;
    }
}
