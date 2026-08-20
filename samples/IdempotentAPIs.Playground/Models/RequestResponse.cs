using System;
namespace IdempotentAPIs.Playground.Models
{
    // Positional record with built-in parameter mappings
    public record PlaceOrderRequest(Guid AccountId, Guid VendorId, Guid ItemId, int Qty);
    public record PlaceOrderResponse(Guid AccountId, Guid OrderId, decimal Amount, decimal CurrentBalance);
}
