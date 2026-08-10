using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdempotentFilterAttributes
{
    public  static class IdempotencyServiceCollectionExtensions
    {
        public static IServiceCollection AddIdempotencyProtection(this IServiceCollection services)
        {
            // Default library implementation (can be swapped for Redis later)
            services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
            return services;
        }

    }
}
