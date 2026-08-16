using System;

namespace IdempotentAPIs.Playground.Domain
{
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;       // e.g., "OrderPlacedEvent"
        public string Content { get; set; } = string.Empty;    // Serialized JSON message payload
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedOn { get; set; }             // Null indicates pending, value indicates completed
        public string? Error { get; set; }                     // Captures publishing failures for dead-letter diagnostics
    }
}
