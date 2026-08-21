
namespace Idempotent.Domain.Models
{
    // Link table mapping that items can be offered by different vendors at different rates
    public class VendorItemPrice
    {
        public Guid VendorId { get; set; }
        public Guid ItemId { get; set; }
        public decimal Price { get; set; }

        public Vendor? Vendor { get; set; }
        public Item? Item { get; set; }
    }
}
