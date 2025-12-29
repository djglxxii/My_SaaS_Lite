using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaaSLite.CloudApi.Data;
using SaaSLite.CloudApi.Data.Entities;
using SaaSLite.Contracts;

var builder = WebApplication.CreateBuilder(args);

// SQLite for PoC
var connectionString =
    builder.Configuration.GetConnectionString("SaaSLite")
    ?? "Data Source=saaslite.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// PoC convenience: auto-create DB on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

// -------------------------
// Health
// -------------------------
app.MapGet("/health", () => Results.Ok(new { ok = true }));

// -------------------------
// Results Ingest (idempotent)
// -------------------------
app.MapPost("/api/results", async (
    [FromBody] ResultIngestRequest request,
    AppDbContext db) =>
{
    // Minimal validation (PoC)
    if (string.IsNullOrWhiteSpace(request.ResultId))
    {
        return Results.BadRequest("ResultId is required.");
    }

    if (string.IsNullOrWhiteSpace(request.SiteId))
    {
        return Results.BadRequest("SiteId is required.");
    }

    if (string.IsNullOrWhiteSpace(request.DeviceId))
    {
        return Results.BadRequest("DeviceId is required.");
    }

    // Idempotency check
    var exists = await db.Results.AnyAsync(r => r.ResultId == request.ResultId);
    if (exists)
    {
        // Update heartbeat anyway (helpful for demos)
        await UpsertDeviceHeartbeatAsync(db, request.SiteId, request.DeviceId, request.EdgeAgentId);
        return Results.Ok(new { status = "duplicate", resultId = request.ResultId });
    }

    var now = DateTime.UtcNow;

    var collected = request.CollectedAtUtc == default ? now : request.CollectedAtUtc;
    if (collected.Kind == DateTimeKind.Unspecified)
    {
        collected = DateTime.SpecifyKind(collected, DateTimeKind.Utc);
    }

    var entity = new ResultEntity
    {
        ResultId = request.ResultId.Trim(),
        SiteId = request.SiteId.Trim(),
        DeviceId = request.DeviceId.Trim(),
        EdgeAgentId = (request.EdgeAgentId).Trim(),

        CollectedAtUtc = collected,
        ReceivedAtUtc = now,

        TestCode = (request.TestCode).Trim(),
        PatientId = (request.PatientId).Trim(),
        OperatorId = (request.OperatorId).Trim(),

        NormalizedJson = string.IsNullOrWhiteSpace(request.NormalizedJson) ? "{}" : request.NormalizedJson,
        RawPayloadJson = string.IsNullOrWhiteSpace(request.RawPayloadJson) ? "{}" : request.RawPayloadJson,
    };

    db.Results.Add(entity);

    await UpsertDeviceHeartbeatAsync(db, entity.SiteId, entity.DeviceId, entity.EdgeAgentId);

    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        // In case of a race, treat as idempotent success
        return Results.Ok(new { status = "duplicate", resultId = request.ResultId });
    }

    return Results.Created($"/api/results/{entity.ResultId}", new { status = "created", resultId = entity.ResultId });
});

// -------------------------
// Results Query (list)
// -------------------------
app.MapGet("/api/results", async (
    [FromQuery] DateTime? fromUtc,
    [FromQuery] DateTime? toUtc,
    [FromQuery] string? deviceId,
    AppDbContext db) =>
{
    var query = db.Results.AsNoTracking().AsQueryable();

    if (fromUtc.HasValue)
    {
        query = query.Where(r => r.CollectedAtUtc >= fromUtc.Value);
    }

    if (toUtc.HasValue)
    {
        query = query.Where(r => r.CollectedAtUtc <= toUtc.Value);
    }

    if (!string.IsNullOrWhiteSpace(deviceId))
    {
        query = query.Where(r => r.DeviceId == deviceId.Trim());
    }

    var list = await query
        .OrderByDescending(r => r.CollectedAtUtc)
        .Take(500) // PoC limit
        .Select(r => new ResultSummaryDto
        {
            ResultId = r.ResultId,
            SiteId = r.SiteId,
            DeviceId = r.DeviceId,
            CollectedAtUtc = r.CollectedAtUtc,
            ReceivedAtUtc = r.ReceivedAtUtc,
            TestCode = r.TestCode,
            PatientId = r.PatientId
        })
        .ToListAsync();

    return Results.Ok(list);
});

// -------------------------
// Results Detail
// -------------------------
app.MapGet("/api/results/{resultId}", async (string resultId, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(resultId))
    {
        return Results.BadRequest("resultId is required.");
    }

    var r = await db.Results.AsNoTracking().FirstOrDefaultAsync(x => x.ResultId == resultId.Trim());
    if (r is null)
    {
        return Results.NotFound();
    }

    // Return full shape similar to Result entity for now (PoC)
    return Results.Ok(new
    {
        r.ResultId,
        r.SiteId,
        r.DeviceId,
        r.EdgeAgentId,
        r.CollectedAtUtc,
        r.ReceivedAtUtc,
        r.TestCode,
        r.PatientId,
        r.OperatorId,
        r.NormalizedJson,
        r.RawPayloadJson
    });
});

// -------------------------
// Heartbeat
// -------------------------
app.MapPost("/api/devices/{deviceId}/heartbeat", async (
    string deviceId,
    [FromQuery] string? siteId,
    [FromQuery] string? deviceType,
    [FromQuery] string? displayName,
    AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(deviceId))
    {
        return Results.BadRequest("deviceId is required.");
    }

    var now = DateTime.UtcNow;

    var existing = await db.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId.Trim());
    if (existing is null)
    {
        existing = new DeviceEntity
        {
            DeviceId = deviceId.Trim(),
            SiteId = (siteId ?? "site-001").Trim(),
            DeviceType = (deviceType ?? "Simulated").Trim(),
            DisplayName = (displayName ?? deviceId.Trim()).Trim(),
            LastSeenUtc = now
        };

        db.Devices.Add(existing);
    }
    else
    {
        if (!string.IsNullOrWhiteSpace(siteId))
        {
            existing.SiteId = siteId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(deviceType))
        {
            existing.DeviceType = deviceType.Trim();
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            existing.DisplayName = displayName.Trim();
        }

        existing.LastSeenUtc = now;
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { status = "ok", deviceId = existing.DeviceId, lastSeenUtc = existing.LastSeenUtc });
});

// -------------------------
// Devices List
// -------------------------
app.MapGet("/api/devices", async (AppDbContext db) =>
{
    var now = DateTime.UtcNow;
    var onlineWindow = TimeSpan.FromMinutes(2);

    var devices = await db.Devices.AsNoTracking()
        .OrderBy(d => d.DeviceId)
        .Select(d => new DeviceDto
        {
            DeviceId = d.DeviceId,
            SiteId = d.SiteId,
            DisplayName = d.DisplayName,
            DeviceType = d.DeviceType,
            LastSeenUtc = d.LastSeenUtc,
            Status = (now - d.LastSeenUtc) <= onlineWindow ? "Online" : "Offline"
        })
        .ToListAsync();

    return Results.Ok(devices);
});

app.Run();

static async Task UpsertDeviceHeartbeatAsync(AppDbContext db, string siteId, string deviceId, string edgeAgentId)
{
    var now = DateTime.UtcNow;

    var d = await db.Devices.FirstOrDefaultAsync(x => x.DeviceId == deviceId);
    if (d is null)
    {
        d = new DeviceEntity
        {
            DeviceId = deviceId,
            SiteId = siteId,
            DeviceType = "Simulated",
            DisplayName = deviceId,
            LastSeenUtc = now
        };
        db.Devices.Add(d);
    }
    else
    {
        d.SiteId = siteId;
        d.LastSeenUtc = now;
    }
}
