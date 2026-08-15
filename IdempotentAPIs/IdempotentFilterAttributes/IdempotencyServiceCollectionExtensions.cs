using IdempotentFilterAttributes.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace IdempotentFilterAttributes
{
    public static class IdempotencyServiceCollectionExtensions
    {
        public static IServiceCollection AddIdempotencyProtection(
            this IServiceCollection services,
            IConfiguration config) // Removed the explicit ILogger parameter
        {
            var section = config.GetSection("IdempotentOptions");
            // Bind options, add validation rule, and ensure it validates immediately at startup
            services.AddOptions<IdempotencyOptions>()
                .Bind(config.GetSection("IdempotentOptions")) // Aligned section name with step 2
                .Validate(o => o.CacheDurationInMinutes > 0, "CacheDurationInMinutes must be greater than 0.")
                .ValidateOnStart();
            // Extract connection string for the Multiplexer setup
            var options = section.Get<IdempotencyOptions>() ?? new IdempotencyOptions();

            // Register StackExchange.Redis ConnectionMultiplexer as a Singleton
            //services.AddSingleton<IConnectionMultiplexer>(sp =>
            //ConnectionMultiplexer.Connect(options.RedisConnectionString));

            // Register an independent ConnectionMultiplexer client for each configured node address
            services.AddSingleton<IEnumerable<IConnectionMultiplexer>>(sp =>
            {
                var clients = new List<IConnectionMultiplexer>();
                foreach (var node in options.RedisNodes)
                {
                    clients.Add(ConnectionMultiplexer.Connect($"{node},abortConnect=false,connectTimeout=1000"));
                }
                return clients;
            });

            // Default infrastructure registration
            //services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
            //services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
            services.AddSingleton<IIdempotencyStore, RedlockDistributedStore>(); 
            // Register the filter itself so it can be resolved inside Program.cs
            services.AddScoped<IdempotentFilter>();

            return services;
        }
    }
}
