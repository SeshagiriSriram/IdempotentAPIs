using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
var app = builder.Build();

app.MapPost("/payments", async (PaymentRequest request, HttpContext context, IMemoryCache cache) =>
{
// 1. Extract the Idempotency Key from request headers
if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) ||
string.IsNullOrWhiteSpace(idempotencyKey))
{
    return Results.BadRequest("Missing required 'Idempotency-Key' header.");
}
string cacheKey = $"idempotency:{idempotencyKey}";
    // 2. Check if this exact key was already processed
if (cache.TryGetValue(cacheKey, out string? cachedResponseJson))
    {
        var cachedResponse = JsonSerializer.Deserialize<PaymentResponse>(cachedResponseJson!);
        // Add a custom header indicating the client received a replayed/cached response
        context.Response.Headers.Add("Idempotency-Replayed", "true");
        return Results.Ok(cachedResponse);
    }
// 3. Process the logic (Simulating database/payment gate processing)
var paymentResponse = new PaymentResponse(
                    Id: Guid.NewGuid(),
                    Amount: request.Amount,
                    Status: "Success",
                    ProcessedAt: DateTime.UtcNow
                );
  // 4. Cache the successful result with a Time-To-Live (TTL) window (e.g., 10 minutes)
   var cacheOptions = new MemoryCacheEntryOptions
   {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
   };
   string serializedResponse = JsonSerializer.Serialize(paymentResponse);
   cache.Set(cacheKey, serializedResponse, cacheOptions);
   return Results.Ok(paymentResponse);
    });

app.Run();
public record PaymentRequest(decimal Amount, string Currency, string AccountNumber);
public record PaymentResponse(Guid Id, decimal Amount, string Status, DateTime ProcessedAt);
