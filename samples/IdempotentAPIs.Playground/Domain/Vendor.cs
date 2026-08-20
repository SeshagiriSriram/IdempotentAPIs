using System;
using System.Collections.Generic;

namespace IdempotentAPIs.Playground.Domain
{
    public class Vendor
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public ICollection<VendorItemPrice> VendorItems { get; set; } = new List<VendorItemPrice>();
    }
}
