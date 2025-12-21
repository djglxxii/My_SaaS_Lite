using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages support. This is a simple page-based model suitable for the PoC UI.
builder.Services.AddRazorPages();

// Register an HttpClient for calling the Cloud API. Base address comes from configuration with a fallback.
builder.Services.AddHttpClient("CloudApi", client =>
{
    var baseUrl = builder.Configuration["CloudApiBaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

// Map Razor Pages endpoints.
app.MapRazorPages();

app.Run();