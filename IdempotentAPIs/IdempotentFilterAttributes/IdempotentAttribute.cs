using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdempotentFilterAttributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class IdempotentAttribute : Attribute, IFilterMetadata
    {
        // Optional per endpoint override (Using standard PascalCase)
        public int CacheDurationInMinutes { get; set; } = 60;       
        public string? CustomHeader { get; set; } = "X-Idempotency-Key";
        public string? RedisConnectionString { get; set; } = string.Empty; 
        public IdempotentAttribute() { }

        public IdempotentAttribute(int cacheDurationInMinutes)
        {
            this.CacheDurationInMinutes = cacheDurationInMinutes;
        }
    }
}
