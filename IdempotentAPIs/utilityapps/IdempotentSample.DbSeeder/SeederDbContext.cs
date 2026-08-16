using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdempotentSample.DbSeeder
{
    public class SeederDbContext : DbContext
    {
        public SeederDbContext(DbContextOptions<SeederDbContext> options) : base(options) { }

        public DbSet<Person> Persons => Set<Person>();
        public DbSet<AccountType> AccountTypes => Set<AccountType>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Vendor> Vendors => Set<Vendor>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<VendorItemPrice> VendorItemPrices => Set<VendorItemPrice>();
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("demo");
            modelBuilder.Entity<VendorItemPrice>().HasKey(vp => new { vp.VendorId, vp.ItemId });
            modelBuilder.Entity<Account>().Property(a => a.Balance).HasPrecision(18, 2);
            modelBuilder.Entity<VendorItemPrice>().Property(vp => vp.Price).HasPrecision(18, 2);
            // FIX 2: Set the required decimal scale precision for the Orders table
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
        }
    }

    public class Person { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
    public class AccountType { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
    public class Account { public Guid Id { get; set; } public Guid PersonId { get; set; } public Guid AccountTypeId { get; set; } public decimal Balance { get; set; } }
    public class Vendor { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
    public class Item { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
    public class VendorItemPrice { public Guid VendorId { get; set; } public Guid ItemId { get; set; } public decimal Price { get; set; } }
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
