namespace IdempotentAPIs.Playground.Models
{
    using System.Collections.Generic;

        public class MessageBrokerOptions : IMessageBrokerOptions
        {
            public List<AmqpEndpointConfig> Endpoints { get; set; } = new();
            public string UserName { get; set; } = "guest";
            public string Password { get; set; } = "guest";
            public int PollingIntervalInSeconds { get; set; } = 2;
            public int MaxRetryThreshold { get; set; } = 3;

            // --- New Backoff Tracking Fields ---
            public string RetryStrategy { get; set; } = "Exponential"; // "Fixed" or "Exponential"
            public int InitialRetryDelayInSeconds { get; set; } = 2;
            public int MaxRetryDelayInSeconds { get; set; } = 60;

            public EndpointConfig? Source { get; set; }
            public EndpointConfig? Target { get; set; }

            // Fallback property mapping for standard interface properties compatibility
            public string HostName => Endpoints.Count > 0 ? Endpoints[0].HostName : "localhost";
        }

        public class AmqpEndpointConfig
        {
            public string HostName { get; set; } = "localhost";
            public int Port { get; set; } = 5672;
        }

    public class EndpointConfig
    {
        public string Type { get; set; } = "Queue"; // Default fallback: "Queue" or "Exchange"
        public string Name { get; set; } = string.Empty;
    }
}
