namespace Idempotent.Domain.Models
{
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

        // --- New Enterprise Tracking State Metrics ---

        // Tracks state transitions: "Pending", "StagedForBroker", "Published", "Failed"
        public string State { get; set; } = "Pending";

        // Checkpoint A: Saved successfully from the API Controller end
        public DateTime? CreatedInDbOn { get; set; } = DateTime.UtcNow;

        // Checkpoint B: Handed off from the background Worker thread to the MQ Network broker
        public DateTime? DispatchedToBrokerOn { get; set; }

        public int RetryCount { get; set; } = 0;
        public string? Error { get; set; }
    }
}
