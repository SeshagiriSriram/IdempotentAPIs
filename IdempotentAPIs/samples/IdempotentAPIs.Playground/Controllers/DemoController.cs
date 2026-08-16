using IdempotentFilterAttributes.Core;
using IdempotentFilterAttributes.Infrastructure; 
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RestApplicationWithFilter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemoController : ControllerBase
    {
        [HttpPost]
        [Idempotent] // Enables protection using the Idempotency-Key header
        public IActionResult CreatePayment([FromBody] PaymentRequest request)
        {
            // Process payment safely here
            var receipt = new { OrderId = request.OrderId, Status = "Success", Timestamp = DateTime.UtcNow };
            return Ok(receipt);
        }
    }
    public record PaymentRequest(string OrderId, decimal Amount);

}
