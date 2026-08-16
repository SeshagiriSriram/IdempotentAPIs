// Execute this isolated initialization command once via a CLI task or script
using IdempotentFilterAttributes.Infrastructure;
using Microsoft.EntityFrameworkCore;

var optionsBuilder = new DbContextOptionsBuilder<CommerceDbContext>();
optionsBuilder.UseSqlServer("Server=localhost,1433;Database=CommerceDb;User Id=sa;Password=YourStrong@Pass123;TrustServerCertificate=True;");

using var context = new CommerceDbContext(optionsBuilder.Options);
DbInitializer.Initialize(context); // Builds CommerceDb and provisions the demo schema safely
