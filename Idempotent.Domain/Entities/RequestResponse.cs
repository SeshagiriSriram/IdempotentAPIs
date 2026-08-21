using Idempotent.Domain.Models;
namespace Idempotent.Domain.Entities
{
    // Positional record with built-in parameter mappings
    public record PlaceOrderRequest(Guid AccountId, Guid VendorId, Guid ItemId, int Qty);
    public class PlaceOrderResponse
    {
        public EnumStatusCode Code = EnumStatusCode.OK;
        public String? ErrMsg = String.Empty;
        public object? ActBody = null;
      } 
}
