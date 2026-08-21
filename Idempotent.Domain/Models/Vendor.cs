namespace Idempotent.Domain.Models
{ 
    public class Vendor
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public ICollection<VendorItemPrice> VendorItems { get; set; } = new List<VendorItemPrice>();
    }
}
