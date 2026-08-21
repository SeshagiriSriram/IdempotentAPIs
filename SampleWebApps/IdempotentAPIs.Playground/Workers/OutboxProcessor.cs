using System.Text;
using IdempotentAPIs.Playground.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client; // Uses modern async v7.2 API

namespace IdempotentAPIs.Playground.Workers
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessor> _logger;
        private readonly MessageBrokerOptions _brokerSettings;
        private readonly ConnectionFactory _connectionFactory;

        private IConnection? _connection;
        private IChannel? _channel;

        private int _connectionFailureCount = 0;

        public OutboxProcessor(
            IServiceProvider serviceProvider,
            ILogger<OutboxProcessor> logger,
            IOptions<MessageBrokerOptions> brokerOptions)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _brokerSettings = brokerOptions.Value ?? throw new ArgumentNullException(nameof(brokerOptions));

            _connectionFactory = new ConnectionFactory
            {
                HostName = _brokerSettings.HostName,
                UserName = _brokerSettings.UserName,
                Password = _brokerSettings.Password
            };
        }

        private TimeSpan CalculateBackoffDelay()
        {
            if (string.Equals(_brokerSettings.RetryStrategy, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                return TimeSpan.FromSeconds(_brokerSettings.InitialRetryDelayInSeconds);
            }

            double calculatedSeconds = _brokerSettings.InitialRetryDelayInSeconds * Math.Pow(2, _connectionFailureCount);
            double finalSeconds = Math.Min(calculatedSeconds, _brokerSettings.MaxRetryDelayInSeconds);

            return TimeSpan.FromSeconds(finalSeconds);
        }
        private async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (_channel != null && _connection != null && _connection.IsOpen)
            {
                _connectionFailureCount = 0;
                return true;
            }

            try
            {
                var amqpEndpoints = _brokerSettings.Endpoints.Select(e =>
                    new AmqpTcpEndpoint(e.HostName, e.Port)).ToList();

                _logger.LogInformation("Connecting to RabbitMQ high-availability cluster endpoints...");

                _connection = await _connectionFactory.CreateConnectionAsync(amqpEndpoints, cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // Initialize Source topologies if defined
                if (_brokerSettings.Source != null && !string.IsNullOrWhiteSpace(_brokerSettings.Source.Name))
                {
                    if (string.Equals(_brokerSettings.Source.Type, "Exchange", StringComparison.OrdinalIgnoreCase))
                    {
                        await _channel.ExchangeDeclareAsync(_brokerSettings.Source.Name, ExchangeType.Fanout, durable: true, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _channel.QueueDeclareAsync(_brokerSettings.Source.Name, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
                    }
                }

                // Initialize Target topologies if defined
                if (_brokerSettings.Target != null && !string.IsNullOrWhiteSpace(_brokerSettings.Target.Name))
                {
                    if (string.Equals(_brokerSettings.Target.Type, "Exchange", StringComparison.OrdinalIgnoreCase))
                    {
                        await _channel.ExchangeDeclareAsync(_brokerSettings.Target.Name, ExchangeType.Fanout, durable: true, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _channel.QueueDeclareAsync(_brokerSettings.Target.Name, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
                    }
                }

                _connectionFailureCount = 0;
                return true;
            }
            catch (Exception ex)
            {
                _connectionFailureCount++; // Increment count to scale backoff delay
                _logger.LogError(ex, "❌ Failed to establish connectivity with RabbitMQ cluster nodes.");
                return false;
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor service activated. Polling interval: {Interval}s.", _brokerSettings.PollingIntervalInSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!await EnsureConnectedAsync(stoppingToken))
                {
                    TimeSpan backoffDelay = CalculateBackoffDelay();
                    _logger.LogWarning("RabbitMQ connection unavailable. Backing off for {Delay}s (Streak: {Count}).", backoffDelay.TotalSeconds, _connectionFailureCount);
                    await Task.Delay(backoffDelay, stoppingToken);
                    continue;
                }

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Idempotent.Infra.Context.CommerceDbContext>();

                try
                {
                    var pendingMessages = await dbContext.OutboxMessages
                        .Where(m => m.State == "Pending")
                        .OrderBy(m => m.OccurredOn)
                        .Take(10)
                        .ToListAsync(stoppingToken);

                    if (pendingMessages.Any() && _brokerSettings.Target != null)
                    {
                        string targetExchange = string.Equals(_brokerSettings.Target.Type, "Exchange", StringComparison.OrdinalIgnoreCase) ? _brokerSettings.Target.Name : string.Empty;
                        string targetRoutingKey = string.Equals(_brokerSettings.Target.Type, "Exchange", StringComparison.OrdinalIgnoreCase) ? string.Empty : _brokerSettings.Target.Name;

                        foreach (var message in pendingMessages)
                        {
                            message.State = "StagedForBroker";
                            message.RetryCount++;

                            try
                            {
                                var body = Encoding.UTF8.GetBytes(message.Content);
                                var properties = new BasicProperties { Persistent = true, MessageId = message.Id.ToString() };

                                await _channel!.BasicPublishAsync(
                                    exchange: targetExchange,
                                    routingKey: targetRoutingKey,
                                    mandatory: true,
                                    basicProperties: properties,
                                    body: body,
                                    cancellationToken: stoppingToken);

                                message.State = "Published";
                                message.DispatchedToBrokerOn = DateTime.UtcNow;
                                message.Error = null;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Publish attempt failed for message: {Id}", message.Id);
                                message.Error = ex.Message;

                                if (message.RetryCount >= _brokerSettings.MaxRetryThreshold)
                                {
                                    _logger.LogCritical("Poison message found! Isolating ID '{Id}' as Failed.", message.Id);
                                    message.State = "Failed";
                                }
                                else
                                {
                                    message.State = "Pending";
                                }
                            }
                        }
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred during the Outbox processing routine.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_brokerSettings.PollingIntervalInSeconds), stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.CloseAsync(cancellationToken);
            if (_connection != null) await _connection.CloseAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
