using Idempotent.Domain.Repositories; 
using IdempotentFilterAttributes.Core;
using Microsoft.AspNetCore.Mvc;
using Idempotent.Domain.Entities;
using Idempotent.Domain.Models;
namespace IdempotentFilterAttributes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderRepository orderRepository,
            ILogger<OrdersController> logger
            )
        {
            _logger = logger;
            _orderRepository = orderRepository; 
        }

        [HttpPost]
        [Idempotent(CacheDurationInMinutes = 60)] // Intercepted by our Redlock Distributed Filter
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            _logger.LogInformation("Processing business transactions for Account: {AccountId}", request.AccountId);
            PlaceOrderResponse resp = await _orderRepository.CreateOrderWithOutboxAsync(request);
             
            switch (resp.Code)
            {
                case EnumStatusCode.OK: return Ok(resp.ActBody); 
                case EnumStatusCode.NOT_FOUND : return NotFound(resp.ErrMsg);
                case EnumStatusCode.NOT_ACCEPTABLE: return BadRequest(resp.ErrMsg); 
            }
            return Ok();   
        }
    }
}
