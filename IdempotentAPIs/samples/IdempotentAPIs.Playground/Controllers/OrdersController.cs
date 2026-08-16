using IdempotentAPIs.Playground.Context;
using IdempotentAPIs.Playground.Domain;
using IdempotentAPIs.Playground.Models;

using IdempotentFilterAttributes.Core;
using IdempotentFilterAttributes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdempotentFilterAttributes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly CommerceDbContext _dbContext;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(CommerceDbContext dbContext, ILogger<OrdersController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost]
        [Idempotent(CacheDurationInMinutes = 60)] // Intercepted by our Redlock Distributed Filter
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            _logger.LogInformation("Processing business transactions for Account: {AccountId}", request.AccountId);

            if (request.Qty <= 0)
            {
                return BadRequest("Order quantity must be greater than zero.");
            }

            // Execute processing operations within a single atomic database context transaction block
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1. Validate Account existence and retrieve user state
                var account = await _dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.AccountId);

                if (account == null)
                {
                    return NotFound($"Account identifier '{request.AccountId}' not found.");
                }

                // 2. Fetch price from the matrix link table based on chosen Vendor and Item setup
                var vendorItemPrice = await _dbContext.VendorItemPrices
                    .FirstOrDefaultAsync(vp => vp.VendorId == request.VendorId && vp.ItemId == request.ItemId);

                if (vendorItemPrice == null)
                {
                    return BadRequest("The selected vendor does not offer this item, or the item details are invalid.");
                }

                // 3. Compute monetary values and verify funding allowances
                decimal totalAmount = vendorItemPrice.Price * request.Qty;

                if (account.Balance < totalAmount)
                {
                    return BadRequest($"Insufficient funds. Required: {totalAmount:C}, Current Balance: {account.Balance:C}");
                }

                // 4. Update the running account checking balance
                account.Balance -= totalAmount;

                // 5. Append a fresh Order tracker entity entry to the ledger
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    AccountId = request.AccountId,
                    VendorId = request.VendorId,
                    ItemId = request.ItemId,
                    Quantity = request.Qty,
                    TotalAmount = totalAmount,
                    PlacedAt = DateTime.UtcNow
                };

                _dbContext.Orders.Add(order);

                // Persist state updates to disk
                await _dbContext.SaveChangesAsync();

                // Commit the relational data transaction atomically
                await transaction.CommitAsync();

                _logger.LogInformation("✅ Order '{OrderId}' placed successfully. Remaining Balance: {Balance}", order.Id, account.Balance);

                // Build output matching the record declaration signature 
                var response = new PlaceOrderResponse(
                    AccountId: account.Id,
                    OrderId: order.Id,
                    Amount: totalAmount,
                    CurrentBalance: account.Balance
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Business logic failed. Rolling back database transaction state updates.");
                await transaction.RollbackAsync();
                return StatusCode(500, "An internal transaction execution breakdown occurred while processing the order.");
            }
        }
    }
}
