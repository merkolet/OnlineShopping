using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using System.IO;
using System.Reflection;
using OrdersService.Services;
using OrdersService.Data;
using Microsoft.EntityFrameworkCore;
using Confluent.Kafka;

namespace OrdersService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(8082);
            });

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Add DbContext for PostgreSQL
            builder.Services.AddDbContext<OrdersDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Configure Kafka Consumer
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
                GroupId = "orders-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            Console.WriteLine($"[LOG] Kafka ConsumerConfig: BootstrapServers={consumerConfig.BootstrapServers}, GroupId={consumerConfig.GroupId}");
            var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            Console.WriteLine("[LOG] Kafka IConsumer created");
            builder.Services.AddSingleton<IConsumer<string, string>>(consumer);

            // Register Services
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IOutboxService, OutboxService>();
            builder.Services.AddHostedService<OutboxProcessorService>();
            builder.Services.AddHostedService<KafkaConsumerService>();
            builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.IncludeXmlComments("/app/OrdersService.xml");
                options.EnableAnnotations();
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Apply database migrations on startup with retry logic
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                int retryCount = 0;
                const int maxRetries = 10;
                const int delaySeconds = 5;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        logger.LogInformation($"Attempting to apply migrations (retry {retryCount + 1}/{maxRetries})...");
                        dbContext.Database.Migrate();
                        logger.LogInformation("Database migrations applied successfully.");
                        break;
                    }
                    catch (Npgsql.NpgsqlException ex)
                    {
                        logger.LogError(ex, $"Database connection failed. Retrying in {delaySeconds} seconds...");
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            logger.LogCritical(ex, "Max retry attempts reached. Could not connect to database.");
                            throw; // Re-throw if max retries reached
                        }
                        Task.Delay(TimeSpan.FromSeconds(delaySeconds)).Wait();
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex, "An unexpected error occurred during database migration.");
                        throw; // Re-throw for other unexpected errors
                    }
                }
            }

            app.MapControllers();

            app.Run();
        }
    }
} 