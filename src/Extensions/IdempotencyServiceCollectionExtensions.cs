using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdempotentFilterAttributes.Models;
using StackExchange.Redis;
using IdempotentFilterAttributes.Infrastructure;
using IdempotentFilterAttributes.Core;

namespace IdempotentFilterAttributes.Extensions
{
    public static class IdempotencyServiceCollectionExtensions
    {
        public static IServiceCollection AddIdempotencyProtection(
            this IServiceCollection services,
            IConfiguration config)
        {
            var section = config.GetSection("IdempotentOptions");

            services.AddOptions<IdempotencyOptions>()
                .Bind(section)
                .ValidateOnStart();

            var options = section.Get<IdempotencyOptions>() ?? new IdempotencyOptions();

            services.AddSingleton<IEnumerable<IConnectionMultiplexer>>(sp =>
            {
                var connectTasks = options.RedisNodes.Select(node =>
                    // FIX: Added explicit low-latency syncTimeout and asyncTimeout limits (in milliseconds)
                    ConnectionMultiplexer.ConnectAsync($"{node},abortConnect=false,connectTimeout=1000,syncTimeout=250,asyncTimeout=250"));

                Task.WaitAll(connectTasks);
                return connectTasks.Select(t => t.Result).ToList();
            });

            services.AddSingleton<IIdempotencyStore, RedlockDistributedStore>();
            services.AddScoped<IdempotentFilter>();

            return services;
        }
    }
}
