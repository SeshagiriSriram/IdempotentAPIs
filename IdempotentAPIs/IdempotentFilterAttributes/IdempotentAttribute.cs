using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection; // <-- add this
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IdempotentFilterAttributes
{
    

    [AttributeUsage(AttributeTargets.Method)]
    public class IdempotentAttribute : Attribute, IAsyncActionFilter
    {


        private static async Task<string> ComputeBodyHashAsync(HttpRequest request)
        {

            request.EnableBuffering(); // Allows reading the stream multiple times

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // Reset pointer for model binder

            byte[] inputBytes = Encoding.UTF8.GetBytes(body);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes);
        }

        private const string HeaderName = "Idempotency-Key";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var _logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<IdempotentAttribute>>();

            // 1. Check for the Idempotency-Key header
            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var extractedKey) ||
                string.IsNullOrWhiteSpace(extractedKey))
            {
                context.Result = new BadRequestObjectResult($"Missing '{HeaderName}' header.");
                return;
            }

            var store = context.HttpContext.RequestServices.GetRequiredService<IIdempotencyStore>();
            string key = extractedKey.ToString();

            string currentRequestHash = await ComputeBodyHashAsync(context.HttpContext.Request);

            // 2. Check if the request was already processed
            var cachedResponse = await store.GetAsync(key);
            
            if (cachedResponse != null)
            {
                _logger.LogInformation("cached Response: {0}", cachedResponse.RequestHash);
                _logger.LogInformation("Actual response: {0}", currentRequestHash); 
                if (cachedResponse.RequestHash == currentRequestHash)
                {
                    context.Result = new BadRequestObjectResult("Idempotency key mismatch: The request body does not match the original request.");
                    return;
                }
                context.Result = new ObjectResult(cachedResponse.Value)
                {
                    StatusCode = cachedResponse.StatusCode
                };
                return; // Short-circuit pipeline and return cached data
            }

            // 3. Acquire lock to prevent race conditions from concurrent retries
            if (!await store.TryLockAsync(key))
            {
                context.Result = new ConflictObjectResult("A request with this key is already processing.");
                return;
            }

            // 4. Execute the actual controller action
            var executedContext = await next();

            // 5. Cache successful responses (2xx status codes)
            if (executedContext.Result is ObjectResult objectResult &&
                objectResult.StatusCode >= 200 && objectResult.StatusCode < 300)
            {
                await store.SaveAsync(key, new CachedResponse
                {
                    StatusCode = objectResult.StatusCode.Value,
                    Value = objectResult.Value, 
                    RequestHash = currentRequestHash
                });
            }
        }
    }
} 