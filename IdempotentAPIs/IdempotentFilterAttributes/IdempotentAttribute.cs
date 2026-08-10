using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace IdempotentFilterAttributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class IdempotentAttribute : Attribute, IAsyncActionFilter
    {
        private const string HeaderName = "Idempotency-Key";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. Check for the Idempotency-Key header
            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var extractedKey) ||
                string.IsNullOrWhiteSpace(extractedKey))
            {
                context.Result = new BadRequestObjectResult($"Missing '{HeaderName}' header.");
                return;
            }

            var store = context.HttpContext.RequestServices.GetRequiredService<IIdempotencyStore>();
            string key = extractedKey.ToString();

            // 2. Check if the request was already processed
            var cachedResponse = await store.GetAsync(key);
            if (cachedResponse != null)
            {
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
                    Value = objectResult.Value
                });
            }
        }
    }
}
