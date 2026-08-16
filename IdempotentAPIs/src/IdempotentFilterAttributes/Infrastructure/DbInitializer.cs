using System;
using System.Linq;
using IdempotentFilterAttributes.Models;

namespace IdempotentFilterAttributes.Infrastructure
{
    public static class DbInitializer
    {
        // Fixed static GUID identifiers for lookups and operational entities
        public static readonly Guid CheckingTypeId = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
        public static readonly Guid SavingsTypeId = Guid.Parse("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");

        public static readonly Guid TestPersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid TestAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid TestVendorAId = Guid.Parse("33333333-3333-3333-3333-33333333333a");
        public static readonly Guid TestVendorBId = Guid.Parse("33333333-3333-3333-3333-33333333333b");
        public static readonly Guid TestItemId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public static void Initialize(CommerceDbContext context)
        {
            // Forces physical database, custom schemas, and tables to deploy if missing
            context.Database.EnsureCreated();

            // Idempotent Verification: Skip writing to disk if structural seed data is already present
            if (context.AccountTypes.Any(at => at.Id == CheckingTypeId))
            {
                return;
            }

            // 1. Populate Account Types Lookups using fixed Guid values
            context.AccountTypes.AddRange(
                new AccountType { Id = CheckingTypeId, Name = "Checking" },
                new AccountType { Id = SavingsTypeId, Name = "Savings" }
            );

            // 2. Populate Test Person and Balance Tracking Accounts
            var person = new Person { Id = TestPersonId, Name = "Alice Smith" };
            context.Persons.Add(person);

            context.Accounts.Add(new Account
            {
                Id = TestAccountId,
                PersonId = TestPersonId,
                AccountTypeId = CheckingTypeId,
                Balance = 5000.00m
            });

            // 3. Populate Vendors and Items
            context.Vendors.AddRange(
                new Vendor { Id = TestVendorAId, Name = "Alpha Logistics" },
                new Vendor { Id = TestVendorBId, Name = "Beta Distribution" }
            );

            context.Items.Add(new Item { Id = TestItemId, Name = "Enterprise Cloud Server" });

            // 4. Populate Matrix Prices
            context.VendorItemPrices.AddRange(
                new VendorItemPrice { VendorId = TestVendorAId, ItemId = TestItemId, Price = 150.00m },
                new VendorItemPrice { VendorId = TestVendorBId, ItemId = TestItemId, Price = 135.00m }
            );

            context.SaveChanges();
        }
    }
}
