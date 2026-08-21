using Microsoft.EntityFrameworkCore;
using Idempotent.Domain.Models; 
namespace Idempotent.Infra.Context
{
    public class CommerceDbContext : DbContext
    {
        public CommerceDbContext(DbContextOptions<CommerceDbContext> options) : base(options) { }

        public DbSet<Person> Persons => Set<Person>();
        public DbSet<AccountType> AccountTypes => Set<AccountType>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Vendor> Vendors => Set<Vendor>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<VendorItemPrice> VendorItemPrices => Set<VendorItemPrice>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // FIX: This forces EF Core to automatically create and assign all tables to the 'demo' schema
            modelBuilder.HasDefaultSchema("demo");

            modelBuilder.Entity<AccountType>()
               .Property(a => a.Id)
                .ValueGeneratedNever(); // Prevents SQL Server from creating an IDENTITY column

            // Configure Composite Key for the item pricing matrix
            modelBuilder.Entity<VendorItemPrice>()
                .HasKey(vp => new { vp.VendorId, vp.ItemId });

            // Enforce proper scale and precision configurations for monetary values
            modelBuilder.Entity<Account>()
                .Property(a => a.Balance).HasPrecision(18, 2);

            modelBuilder.Entity<VendorItemPrice>()
                .Property(vp => vp.Price).HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<OutboxMessage>(builder =>
            {
                builder.HasKey(m => m.Id);
                builder.Property(m => m.Type).HasMaxLength(256).IsRequired();
                builder.Property(m => m.Content).IsRequired();
            });
        }
    }
}
