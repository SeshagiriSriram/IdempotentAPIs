
using IdempotentFilterAttributes;

namespace RestApplicationWithFilter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            //app.UseHttpsRedirection();

            //app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
