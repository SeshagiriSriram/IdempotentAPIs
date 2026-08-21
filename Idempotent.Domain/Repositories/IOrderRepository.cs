using Idempotent.Domain.Entities;
using Idempotent.Domain.Models; 
namespace Idempotent.Domain.Repositories
{
    public interface IOrderRepository
    {
        // Task CreateOrderEntryAsync(Order order);
        Task <Account?>FindAccountById(Guid actId);
        Task<VendorItemPrice?> FindVendorItemById(Guid vendorId, Guid itemId);
        Task<PlaceOrderResponse> ValidateAsync(PlaceOrderRequest request); 
        // for outbox ...
        Task<PlaceOrderResponse> CreateOrderWithOutboxAsync(PlaceOrderRequest request); 
    }
}
