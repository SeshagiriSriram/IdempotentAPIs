using IdempotentAPIs.Playground.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Idempotent.Domain.Repositories;
using Idempotent.Infra.Context;
using Idempotent.Infra.Repositories;
using IdempotentFilterAttributes.Filters;
using IdempotentFilterAttributes.Extensions;
using IdempotentAPIs.Playground.Workers;

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
                options.UseSqlServer(standardConnectionString)
                .UseLoggerFactory(LoggerFactory.Create(builder =>
                {
                    builder
                        .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                }))); 

            builder.Services.AddIdempotencyProtection(builder.Configuration);
            builder.Services.AddMemoryCache();

            builder.Services.AddHostedService<OutboxProcessor>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            // 3. Register Filter globally inside the MVC controller engine
            builder.Services.AddControllers(options =>
            {
                // Evaluates every request but short-circuits ONLY if [Idempotent] attribute exists
                //options.Filters.Add<IdempotentFilter>();
                options.Filters.Add(new TypeFilterAttribute(typeof(IdempotentFilter)));
            });


            builder.Services.AddOptions<MessageBrokerOptions>()
        .Bind(builder.Configuration.GetSection("MessageBrokerOptions"))
        // FIX: Use standard PostConfigure so the options object can be cast and validated safely
        .PostConfigure(options =>
        {
            // Cast the concrete option class to its validation interface contract 
            if (options is IMessageBrokerOptions validatableOptions)
            {
                // Executes the Default Interface Method validation check on application startup
                validatableOptions.Validate();
            }
        })
        .ValidateOnStart(); // Hard-crashes immediately if appsettings lacks Source AND Target parameters



            // 2. 🚀 THE FIX: Force the container to collapse both maps into a single Scoped registration slot

            var existingRegistration = builder.Services
    .FirstOrDefault(d => d.ServiceType == typeof(IdempotentFilterAttributes.Context.IIdempotencyContext));

            if (existingRegistration != null)
            {
                builder.Services.Remove(existingRegistration);
            }

            // 3. Now register the implementation explicitly as Scoped
            builder.Services.AddScoped<IdempotentFilterAttributes.Context.IdempotencyContext>();

            // 4. Map the interface to resolve the EXACT same concrete class instance as above
            builder.Services.AddScoped<IdempotentFilterAttributes.Context.IIdempotencyContext>(provider =>
                provider.GetRequiredService<IdempotentFilterAttributes.Context.IdempotencyContext>());


            var app = builder.Build();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
