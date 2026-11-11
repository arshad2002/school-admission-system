using AdmissionSystem.Api.Data;
using AdmissionSystem.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Resolve connection string from ConnectionStrings:DefaultConnection or DATABASE_URL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                      ?? builder.Configuration["DATABASE_URL"];
    connectionString = ParseDatabaseUrl(databaseUrl);
}

// Register PostgreSQL Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register CORS policy for frontend communication
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register application services
builder.Services.AddScoped<AdmissionService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Required for OpenAPI to discover Controller endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Automatically execute EF Core migrations at runtime on startup with retry logic
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Applying EF Core migrations at startup...");
            db.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning(ex, "Failed to apply migrations. Retrying in 3 seconds... ({Retries} attempts remaining)", retries);
            if (retries == 0) throw;
            Thread.Sleep(3000);
        }
    }
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Visual UI at /scalar/v1
}

if (!app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("EnableHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// Health check endpoint for Docker & container orchestration
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

static string? ParseDatabaseUrl(string? url)
{
    if (string.IsNullOrWhiteSpace(url)) return null;
    if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return url;
    }

    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');
    return $"Host={host};Port={port};Database={database};Username={user};Password={password};";
}