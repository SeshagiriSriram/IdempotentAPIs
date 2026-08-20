using IdempotentSample.DbSeeder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;

namespace IdempotentAPIs.DataSeeder
{
    class Program
    {
        private const string PrimaryConnectionString = "Server=localhost,1433;Database=CommerceDb;User Id=sa;Password=YourStrong@Pass123;TrustServerCertificate=True;";

        private static readonly Guid CheckingTypeId = Guid.Parse("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
        private static readonly Guid SavingsTypeId = Guid.Parse("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
        private static readonly Guid TestPersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid TestAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid TestVendorAId = Guid.Parse("33333333-3333-3333-3333-33333333333a");
        private static readonly Guid TestVendorBId = Guid.Parse("33333333-3333-3333-3333-33333333333b");
        private static readonly Guid TestItemId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        static void Main(string[] args)
        {
            Console.WriteLine("⏳ Starting standalone database provisioning utility...");

            try
            {
                // 1. Extract the actual targeted database name ('CommerceDb')
                var primaryBuilder = new SqlConnectionStringBuilder(PrimaryConnectionString);
                string targetDatabaseName = primaryBuilder.InitialCatalog;

                // 2. Build the master-route connection string to pass the login check
                var masterBuilder = new SqlConnectionStringBuilder(PrimaryConnectionString)
                {
                    InitialCatalog = "master"
                };

                var masterOptions = new DbContextOptionsBuilder<SeederDbContext>()
                    .UseSqlServer(masterBuilder.ConnectionString, sqlOptions =>
                        sqlOptions.EnableRetryOnFailure(6, TimeSpan.FromSeconds(5), null))
                    .Options;

                using (var masterContext = new SeederDbContext(masterOptions))
                {
                    Console.WriteLine("Authenticated via master. Checking catalog presence on server...");

                    // Fetch EF Core's internal relational service layer
                    var databaseCreator = masterContext.Database.GetService<IRelationalDatabaseCreator>();

                    // Execute a safe raw check to see if the catalog exists
                    bool databaseExists = masterContext.Database.ExecuteSqlRaw(
                        $"SELECT COUNT(*) FROM sys.databases WHERE name = '{targetDatabaseName}'") > 0;

                    if (!databaseExists)
                    {
                        Console.WriteLine($"Database '{targetDatabaseName}' not found. Spawning new catalog...");
                        // Explicitly issues a clean "CREATE DATABASE [CommerceDb]" from the master session
                        masterContext.Database.ExecuteSqlRaw($"CREATE DATABASE [{targetDatabaseName}]");
                        Console.WriteLine($"Catalog '{targetDatabaseName}' created successfully.");
                    }
                }

                // 3. Now that the physical catalog exists, pivot to the primary string to map schemas and tables
                var runtimeOptions = new DbContextOptionsBuilder<SeederDbContext>()
                    .UseSqlServer(PrimaryConnectionString)
                    .Options;

                using (var runtimeContext = new SeederDbContext(runtimeOptions))
                {
                    Console.WriteLine("Building database tables and schemas inside the 'demo' namespace...");

                    // Since CommerceDb exists now, this maps the tables cleanly to it instead of master!
                    runtimeContext.Database.EnsureCreated();

                    if (runtimeContext.AccountTypes.Any(at => at.Id == CheckingTypeId))
                    {
                        Console.WriteLine("✅ Database already contains baseline data rows. Seeding skipped.");
                        return;
                    }

                    Console.WriteLine("Populating core system lookup structures...");

                    runtimeContext.AccountTypes.AddRange(
                        new AccountType { Id = CheckingTypeId, Name = "Checking" },
                        new AccountType { Id = SavingsTypeId, Name = "Savings" }
                    );

                    runtimeContext.Persons.Add(new Person { Id = TestPersonId, Name = "Alice Smith" });
                    runtimeContext.Accounts.Add(new Account
                    {
                        Id = TestAccountId,
                        PersonId = TestPersonId,
                        AccountTypeId = CheckingTypeId,
                        Balance = 5000.00m
                    });

                    runtimeContext.Vendors.AddRange(
                        new Vendor { Id = TestVendorAId, Name = "Alpha Logistics" },
                        new Vendor { Id = TestVendorBId, Name = "Beta Distribution" }
                    );
                    runtimeContext.Items.Add(new Item { Id = TestItemId, Name = "Enterprise Cloud Server" });

                    runtimeContext.VendorItemPrices.AddRange(
                        new VendorItemPrice { VendorId = TestVendorAId, ItemId = TestItemId, Price = 150.00m },
                        new VendorItemPrice { VendorId = TestVendorBId, ItemId = TestItemId, Price = 135.00m }
                    );

                    runtimeContext.SaveChanges();
                    Console.WriteLine("🎉 Standalone initialization completed successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Setup Stopped: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Details: {ex.InnerException.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }
    }

    // (Keep your SeederDbContext and lightweight entities exactly the same below this line)
}
