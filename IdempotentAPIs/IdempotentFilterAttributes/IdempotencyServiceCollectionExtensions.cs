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
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(options.RedisConnectionString));

            // Default infrastructure registration
            //services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
            services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
            // Register the filter itself so it can be resolved inside Program.cs
            services.AddScoped<IdempotentFilter>();

            return services;
        }
    }
}
