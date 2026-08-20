using System;
using System.Collections.Generic;

namespace IdempotentAPIs.Playground.Domain
{
    public class Item
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public ICollection<VendorItemPrice> VendorOffering { get; set; } = new List<VendorItemPrice>();
    }
}
