using IdempotentFilterAttributes.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace IdempotentFilterAttributes
{
    public class IdempotentFilter : IAsyncResourceFilter
    {
        private readonly ILogger<IdempotentFilter> _logger;
        private readonly IIdempotencyStore _store;
        private readonly IdempotencyOptions _options;

        public IdempotentFilter(
            ILogger<IdempotentFilter> logger,
            IIdempotencyStore store,
            IOptions<IdempotencyOptions> options)
        {
            _logger = logger;
            _store = store;
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        private static async Task<string> ComputeBodyHashAsync(HttpRequest request)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            byte[] inputBytes = Encoding.UTF8.GetBytes(body);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes);
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            // Fix 2: Reliably read metadata from ActionDescriptor instead of context.Filters
            IdempotentAttribute? attr = null;

            if (context.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerDescriptor)
            {
                attr = controllerDescriptor.MethodInfo
                    .GetCustomAttributes(typeof(IdempotentAttribute), inherit: true)
                    .FirstOrDefault() as IdempotentAttribute;
            }

            // If the attribute is missing, skip the idempotency check entirely
            if (attr == null)
            {
                _logger.LogInformation("API is not marked as Idempotent. Normal behaviour.");
                await next();
                return;
            }

            //string connString = _options.RedisConnectionString;
            List<string> redisNodes = _options.RedisNodes;
            foreach (string redisNode in redisNodes)
            {
                _logger.LogInformation("✅ Redis Connections Distributed: {redisNode}", redisNode);
            }

            // Fix 3: Resolve header fallback hierarchy (Attribute override -> AppSettings Configuration)
            string headerName = !string.IsNullOrWhiteSpace(attr.CustomHeader)
                ? attr.CustomHeader
                : _options.HeaderName;

            // 1. Check for the Idempotency header
            if (!context.HttpContext.Request.Headers.TryGetValue(headerName, out var extractedKey) ||
                string.IsNullOrWhiteSpace(extractedKey))
            {
                _logger.LogError("❌ Missing idempotency header: {HeaderName}", headerName);
                context.Result = new BadRequestObjectResult($"Missing '{headerName}' header.");
                return;
            }

            string key = extractedKey.ToString();
            string currentRequestHash = await ComputeBodyHashAsync(context.HttpContext.Request);
            
            // 2. Check if the request was already processed
            var cachedResponse = await _store.GetAsync(key);

            if (cachedResponse != null)
            {
                _logger.LogInformation("Cached Hash: {Cached}, Actual Hash: {Actual}", cachedResponse.RequestHash, currentRequestHash);

                if (!string.Equals(cachedResponse.RequestHash, currentRequestHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("❌ Mismatched Request Hash between original and new request for same key: {key}", key);
                    context.Result = new BadRequestObjectResult("Idempotency key mismatch: The request body does not match the original request.");
                    return;
                }

                context.Result = new ContentResult
                {
                    Content = cachedResponse.Content,
                    ContentType = "application/json",
                    StatusCode = cachedResponse.StatusCode
                };


                // Fix 4: Used modernized dictionary accessor for safety
                context.HttpContext.Response.Headers["Idempotency-Match"] = "true";
                _logger.LogWarning("🚩Cached Response being returned.");
                return;
            }

            // 3. Acquire lock to prevent race conditions from concurrent retries
            if (!await _store.TryLockAsync(key))
            {
                _logger.LogError("❌ Lock cannot be acquired for key: {Key}. Request already processing.", key);
                context.Result = new ConflictObjectResult("A request with this key is already processing.");
                return;
            }

            ResourceExecutedContext executedContext;
            try
            {
                // 4. Execute the actual controller action inside a try block
                executedContext = await next();
            }
            finally
            {
                // Fix 5: Always unlock the key so subsequent retries aren't locked out forever
                await _store.ReleaseLockAsync(key);
            }

            // 5. Cache successful responses (2xx status codes)
            if (executedContext.Result is ObjectResult objectResult &&
                objectResult.StatusCode >= 200 && objectResult.StatusCode < 300)
            {
                // Fix 6: Resolve CacheDuration fallback hierarchy (Attribute explicit parameter value -> AppSettings configuration)
                int finalDuration = 
                    attr.CacheDurationInMinutes > 0
                    ? attr.CacheDurationInMinutes
                    : _options.CacheDurationInMinutes;

                string serializedContent = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
                await _store.SaveAsync(key, new CachedResponse
                {
                    StatusCode = objectResult.StatusCode.Value,
                    Content = serializedContent, 
                    RequestHash = currentRequestHash
                }, finalDuration); // Make sure your store accepts the final duration parameter here!

                _logger.LogInformation("Saved in cache with TTL of {Duration} minutes", finalDuration);
            }
            else
            {
                _logger.LogInformation("Direct Execution of API completed without caching due to failure status code.");
            }
        }
    }

}



