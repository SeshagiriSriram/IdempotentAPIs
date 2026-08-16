using System;
using System.Collections.Generic;

namespace IdempotentFilterAttributes.Models
{
    public class Person
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }

    public class AccountType
    {
        public Guid Id { get; set; } // GUID based Account Type
        public string Name { get; set; } = string.Empty;
    }

    public class Account
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PersonId { get; set; }
        public Guid AccountTypeId { get; set; }
        public decimal Balance { get; set; }

        public Person? Person { get; set; }
        public AccountType? AccountType { get; set; }
    }

    public class Vendor
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public ICollection<VendorItemPrice> VendorItems { get; set; } = new List<VendorItemPrice>();
    }

    public class Item
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public ICollection<VendorItemPrice> VendorOffering { get; set; } = new List<VendorItemPrice>();
    }

    // Link table mapping that items can be offered by different vendors at different rates
    public class VendorItemPrice
    {
        public Guid VendorId { get; set; }
        public Guid ItemId { get; set; }
        public decimal Price { get; set; }

        public Vendor? Vendor { get; set; }
        public Item? Item { get; set; }
    }

    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public Guid VendorId { get; set; }
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

        public Account? Account { get; set; }
    }
}
