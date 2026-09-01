using Microsoft.EntityFrameworkCore;
using SecureConfigApi.Data;
using SecureConfigApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Secure Config API", Version = "v1" });
});

builder.Services.AddScoped<IEncryptionService, AesEncryptionService>();
builder.Services.AddScoped<IConfigService, ConfigService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = System.Environment.GetEnvironmentVariable("DATABASE_URL");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        // Render provides DATABASE_URL for managed Postgres instances.
        options.UseNpgsql(databaseUrl);
    }
    else
    {
        options.UseSqlite(connectionString ?? "Data Source=secureconfig.db");
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

public partial class Program { } // exposed for WebApplicationFactory in integration tests
