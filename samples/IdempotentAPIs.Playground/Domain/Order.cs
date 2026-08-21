using System;
using System.Collections.Generic;

namespace IdempotentAPIs.Playground.Domain
{
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LedgerId { get; set; }
        public Guid AccountId { get; set; }
        public Guid VendorId { get; set; }
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

        public Account? Account { get; set; }
        public string Status { get; set; } = "Pending"; 
    }
}
