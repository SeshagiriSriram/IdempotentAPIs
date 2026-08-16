
using IdempotentFilterAttributes;
using IdempotentFilterAttributes.Core; 
using IdempotentFilterAttributes.Extensions;
using IdempotentFilterAttributes.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IdempotentAPIs.Playground
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Regular registration stays exactly the same for runtime queries
            string? standardConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<CommerceDbContext>(options =>
                options.UseSqlServer(standardConnectionString));


            // ... (retain your existing Redlock initialization statements here) ...
            // Add services to the container.

            // Add services to the container.
            // Register your idempotency store
            //builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

            builder.Services.AddMemoryCache();

            // 2. Custom Library Hook
            // Automatically binds AppSettings options, validates them, and maps the Store + Filter
            builder.Services.AddIdempotencyProtection(builder.Configuration);

            // 3. Register Filter globally inside the MVC controller engine
            builder.Services.AddControllers(options =>
            {
                // Evaluates every request but short-circuits ONLY if [Idempotent] attribute exists
                options.Filters.Add<IdempotentFilter>();
            });



            //builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();
            // now run the initializer... 
            // 2. Upgraded Database Provisioning & Seeding Block
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    // Use SqlConnectionStringBuilder to safely swap the database target to 'master' for initialization
                    var connectionBuilder = new SqlConnectionStringBuilder(standardConnectionString);
                    connectionBuilder.InitialCatalog = "master"; // Directs the login trap away from the missing DB
                    string masterConnectionString = connectionBuilder.ConnectionString;

                    logger.LogInformation("Connecting to master database to verify/provision catalog schema...");

                    // Create a separate DbContextOptions instance bound to the master database connection
                    var optionsBuilder = new DbContextOptionsBuilder<CommerceDbContext>();
                    optionsBuilder.UseSqlServer(masterConnectionString);

                    using var initContext = new CommerceDbContext(optionsBuilder.Options);

                    // This will now successfully login via master, check if CommerceDb exists, and create it safely!
                    initContext.Database.EnsureCreated();

                    // 3. Trigger Data Seeding utilizing the regular application container context
                    var runtimeContext = services.GetRequiredService<CommerceDbContext>();
                    DbInitializer.Initialize(runtimeContext);

                    logger.LogInformation("✅ Database provisioning, demo schema creation, and seeding completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ An error occurred during database structural initialization.");
                }
            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
