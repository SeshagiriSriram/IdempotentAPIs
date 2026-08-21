using Idempotent.Domain.Entities;
using Idempotent.Domain.Models;
using Idempotent.Domain.Repositories;
using Idempotent.Infra.Context;
using IdempotentFilterAttributes.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Idempotent.Infra.Repositories
{
    public class OrderRepository:IOrderRepository
    {
        private readonly CommerceDbContext _context;
        private readonly IIdempotencyContext _idContext; 
        private readonly ILogger _logger;

        public OrderRepository(CommerceDbContext context, 
            IIdempotencyContext idContext,
            ILogger<OrderRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _idContext = idContext ?? throw new ArgumentNullException(nameof(idContext));
            _logger = logger; 
        }

        public async Task<PlaceOrderResponse> ValidateAsync(PlaceOrderRequest request)
        {
            _logger.LogInformation("******* Starting Validation"); 
            PlaceOrderResponse resp = new PlaceOrderResponse(); 

            if (request == null) throw new ArgumentNullException(); 
            if (request.Qty <= 0)
            {
                resp.Code = EnumStatusCode.NOT_ACCEPTABLE;
                resp.ErrMsg = $"Business Logic -Qty {request.Qty} should be >=0";
                resp.ActBody = request; 
                return resp; 
            } // BAD REQUEST 
            var acct = await FindAccountById(request.AccountId);
            if (acct == null)
            {
                _logger.LogInformation("❌ No Account Found");
                resp.Code = EnumStatusCode.NOT_FOUND; 
                resp.ErrMsg = $"No Account found for {request.AccountId}";
                resp.ActBody = request;
                return resp;
            }
            var vendorPriceitem = await FindVendorItemById(request.VendorId, request.ItemId);
            if (vendorPriceitem == null)
            {
                _logger.LogInformation("❌ Vendor is not found or item is not found or Vendor does not supply item");
                resp.Code = EnumStatusCode.NOT_FOUND;
                resp.ErrMsg = $"Vendor {request.VendorId} does not provide item:  {request.ItemId}";
                resp.ActBody = request;
                return resp;
            }
            decimal totalAmount = vendorPriceitem.Price * request.Qty;
            decimal newbal = (acct.Balance - totalAmount); 
            if (acct.Balance < totalAmount)
            {
                _logger.LogInformation("❌ Negative Balance");
                resp.Code = EnumStatusCode.NOT_ACCEPTABLE;
                resp.ErrMsg = $"Current Balance: {acct.Balance}, new Balance will be negative {newbal}";
                resp.ActBody = request;
                return resp;
            }
            _logger.LogInformation("✅ OK. Order is placed for further processing"); 
            resp.ErrMsg = String.Empty;
            resp.ActBody = request; 
            resp.Code = EnumStatusCode.OK; 
            return resp; 
        }

        public async Task<Domain.Models.Account?> FindAccountById(Guid actId)
        {
            var account = await 
                _context.Accounts
                        .FirstOrDefaultAsync(a => a.Id == actId);
            return account; 
        }
        public async Task<VendorItemPrice?> FindVendorItemById(Guid vendorId, Guid itemId)
        {
            var vendorItemPrice = await _context.VendorItemPrices
                        .FirstOrDefaultAsync(vp => vp.VendorId == vendorId && vp.ItemId == itemId);
            return vendorItemPrice; 
        }

        public async Task <PlaceOrderResponse> CreateOrderWithOutboxAsync(PlaceOrderRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (_idContext.LedgerId == Guid.Empty)
            {
                throw new InvalidOperationException("Cannot persist order. Idempotency tracking LedgerId was not initialized.");
            }
            PlaceOrderResponse resp  = new PlaceOrderResponse();
            resp.ActBody = request;
            resp.ErrMsg = String.Empty;
            resp.Code = EnumStatusCode.OK; // defaults...
            resp = await ValidateAsync(request);
            
            if (resp == null)
            {
                throw new InvalidOperationException("Cannot perform validations");
            }
            _logger.LogInformation($"********* code: {resp.Code}");

            if (resp.Code != EnumStatusCode.OK)
            {
                return resp; 
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // _idContext.LedgerId = Guid.NewGuid();
                var order = new Order();
                order.AccountId = request.AccountId;
                order.VendorId = request.VendorId;
                order.ItemId = request.ItemId;
                order.Quantity = request.Qty;
                order.TransactionId = _idContext.LedgerId;
                var account = await _context.Accounts
                    .FromSqlRaw("SELECT * FROM [demo].Accounts WITH (XLOCK, ROWLOCK) WHERE Id = {0}", order.AccountId)
                .FirstOrDefaultAsync();

                // 🚀 3. Defensive Guard: Check if it was deleted between the controller check and now
                if (account == null)
                {
                    throw new InvalidOperationException("Account was deleted or does not exist.");
                }

                // 🚀 4. Check for sufficient funds one last time inside the protected bubble
                if (account.Balance - request.Qty < 0)
                {
                    throw new InvalidOperationException("Insufficient funds discovered during processing execution block.");
                }
                var vendorPriceItem = await FindVendorItemById(request.VendorId, request.ItemId);
                if (vendorPriceItem == null)
                {
                    throw new InvalidOperationException("Vendor/item Mismatch");
                } 
                decimal totalAmount = vendorPriceItem.Price * request.Qty;
                order.TotalAmount = totalAmount;
                account.Balance -= totalAmount; 
                _context.Add(order);
                // _context.Add(account); 
                // now the rest...
                var orderPlacedEvent = new
                {
                    TransactionId = _idContext.LedgerId.ToString(),
                    OrderId = order.Id,
                    order.AccountId,
                    order.VendorId,
                    order.ItemId,
                    Amount = totalAmount
                };
                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = "OrderPlacedEvent",
                    Content = System.Text.Json.JsonSerializer.Serialize(orderPlacedEvent),
                    OccurredOn = DateTime.UtcNow,

                    // --- New Auditing Fields Configuration ---
                    State = "Pending",                 // Marked as Pending so the background worker picks it up
                    CreatedInDbOn = DateTime.UtcNow,   // Checkpoint A: Saved successfully from the API Controller end
                    DispatchedToBrokerOn = null,       // Cleared out initially until the background worker thread runs
                    RetryCount = 0,                    // Starts clean at 0 attempts
                    Error = null                       // No error logs at point of generation
                };
                _context.OutboxMessages.Add(outboxMessage);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                resp.Code = EnumStatusCode.OK;
                resp.ErrMsg = "";
                return resp;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            } 

            // do full transactiom...
        }
 
        public CommerceDbContext getDBContext()
        {
            return _context;
        }
        public IIdempotencyContext getIdContext()
        {
            return _idContext; 
        }

    }
}
