using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;
using PaymentsService.Kafka;
using PaymentsService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/payments-service.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Add DbContext for PostgreSQL
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Kafka Producer Service
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

// Add Services for Transactional Outbox/Inbox
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<OutboxService>();
builder.Services.AddScoped<InboxService>();
builder.Services.AddHostedService<OutboxProcessorService>();
builder.Services.AddHostedService<KafkaConsumerService>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
});

// Add Controllers
builder.Services.AddControllers(); 

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080); // Listen on port 8080 for HTTP requests
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// Apply database migrations on startup with retry logic
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
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
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An unexpected error occurred during database migration.");
            throw; // Re-throw for other unexpected errors
        }
    }
}

// Map Controllers
app.MapControllers(); 

app.Run();