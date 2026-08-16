using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IdempotentAPIs.Playground.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client; // v7.2

namespace IdempotentAPIs.Playground.BackgroundWorkers
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessor> _logger;
        private readonly ConnectionFactory _connectionFactory;

        private IConnection? _connection;
        private IChannel? _channel; // v7: Changed from IModel to IChannel

        public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Configure connection definitions inside constructor safely
            _connectionFactory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };
        }

        private async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (_channel != null && _connection != null && _connection.IsOpen)
            {
                return true;
            }

            try
            {
                _logger.LogInformation("Establishing async network connection to RabbitMQ Broker...");

                // v7: Async connection handshake instantiation
                _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // v7: Async topological configuration deployment
                await _channel.QueueDeclareAsync(
                    queue: "orders.placed",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("✅ Connected to RabbitMQ successfully. Topology initialized.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to connect to RabbitMQ broker node. Retrying in background...");
                return false;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor background task activated.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Ensure broker pipes are ready before processing database layers
                if (!await EnsureConnectedAsync(stoppingToken))
                {
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();

                try
                {
                    // Fetch up to 10 pending records at a time
                    var pendingMessages = await dbContext.OutboxMessages
                        .Where(m => m.ProcessedOn == null)
                        .OrderBy(m => m.OccurredOn)
                        .Take(10)
                        .ToListAsync(stoppingToken);

                    if (pendingMessages.Any())
                    {
                        _logger.LogInformation("Processing {Count} pending outbox items...", pendingMessages.Count);

                        foreach (var message in pendingMessages)
                        {
                            try
                            {
                                var body = Encoding.UTF8.GetBytes(message.Content);

                                // v7: Create modern properties object
                                var properties = new BasicProperties
                                {
                                    Persistent = true, // Survers broker crashes/reboots
                                    MessageId = message.Id.ToString()
                                };

                                // v7: Async broadcast distribution call 
                                await _channel!.BasicPublishAsync(
                                    exchange: string.Empty,
                                    routingKey: "orders.placed",
                                    mandatory: true,
                                    basicProperties: properties,
                                    body: body,
                                    cancellationToken: stoppingToken);

                                message.ProcessedOn = DateTime.UtcNow;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed publishing event payload target: {Id}", message.Id);
                                message.Error = ex.Message;
                            }
                        }

                        // Persist task state updates back to SQL database context
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred during the Outbox processing run.");
                }

                // Poll every 2 seconds
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Shutting down Outbox Background Processor worker thread...");

            if (_channel != null) await _channel.CloseAsync(cancellationToken);
            if (_connection != null) await _connection.CloseAsync(cancellationToken);

            await base.StopAsync(cancellationToken);
        }
    }
}
