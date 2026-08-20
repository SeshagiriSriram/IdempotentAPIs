using System;

namespace IdempotentAPIs.Playground.Models
{
    public interface IMessageBrokerOptions
    {

        string HostName { get; }
        string UserName { get; }
        string Password { get; }
        int PollingIntervalInSeconds { get; }

        EndpointConfig? Source { get; }
        EndpointConfig? Target { get; }

        // Default Interface Method validating structural parameters
        public void Validate()
        {
            // Verify if both endpoint options are absent
            if ((Source == null || string.IsNullOrWhiteSpace(Source.Name)) &&
                (Target == null || string.IsNullOrWhiteSpace(Target.Name)))
            {
                throw new InvalidOperationException(
                    "Invalid MessageBrokerOptions configuration: At least one valid Source or Target endpoint configuration parameter must be provided.");
            }
        }
    }
}
