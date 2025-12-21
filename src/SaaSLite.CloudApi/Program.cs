using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaaSLite.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Configure SQLite database. For demo purposes this uses a file-based SQLite DB.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=saaslite.db"));

// Add API explorer and Swagger to help with testing during development.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger UI in development to explore the endpoints.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Minimal API endpoint to ingest results. Idempotent by ResultId.
app.MapPost("/api/results", async (Result result, AppDbContext db) =>
{
    // Check if the result already exists.
    var existing = await db.Results.FindAsync(result.ResultId);
    if (existing != null)
    {
        return Results.Ok(existing);
    }
    db.Results.Add(result);
    await db.SaveChangesAsync();
    return Results.Created($"/api/results/{result.ResultId}", result);
});

// Query results with optional filters for device and date range.
app.MapGet("/api/results", async (string? deviceId, DateTime? fromUtc, DateTime? toUtc, AppDbContext db) =>
{
    var query = db.Results.AsQueryable();
    if (!string.IsNullOrEmpty(deviceId))
    {
        query = query.Where(r => r.DeviceId == deviceId);
    }
    if (fromUtc != null)
    {
        query = query.Where(r => r.CollectedAtUtc >= fromUtc);
    }
    if (toUtc != null)
    {
        query = query.Where(r => r.CollectedAtUtc <= toUtc);
    }
    return await query.ToListAsync();
});

// Get a single result by id.
app.MapGet("/api/results/{id}", async (string id, AppDbContext db) =>
{
    var result = await db.Results.FindAsync(id);
    return result is not null ? Results.Ok(result) : Results.NotFound();
});

// Return all known devices.
app.MapGet("/api/devices", async (AppDbContext db) => await db.Devices.ToListAsync());

// Heartbeat endpoint for devices. Updates LastSeenUtc and Status.
app.MapPost("/api/devices/{deviceId}/heartbeat", async (string deviceId, AppDbContext db) =>
{
    var device = await db.Devices.FindAsync(deviceId);
    if (device == null)
    {
        device = new Device
        {
            DeviceId = deviceId,
            SiteId = "site-001",
            DisplayName = deviceId,
            DeviceType = "Unknown",
            Status = "Online"
        };
        db.Devices.Add(device);
    }
    device.LastSeenUtc = DateTime.UtcNow;
    device.Status = "Online";
    await db.SaveChangesAsync();
    return Results.Ok(device);
});

app.Run();

// Simple EF Core DbContext for devices and results.
class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Result> Results => Set<Result>();
}