using System;
using SaaSLite.Contracts;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

// Basic simulator for edge agent generating results and uploading to the Cloud API.
var deviceId = "device-001";
var edgeAgentId = "edge-001";

var httpClient = new HttpClient
{
    BaseAddress = new Uri(Environment.GetEnvironmentVariable("CLOUD_API_BASE_URL") ?? "http://localhost:5000")
};

// ConcurrentQueue to hold pending results for upload. In a real implementation this would be persisted to disk.
var queue = new BlockingCollection<Result>();

// Task to simulate generating results periodically.
_ = Task.Run(async () =>
{
    int counter = 1;
    while (true)
    {
        var result = new Result
        {
            ResultId = Guid.NewGuid().ToString(),
            SiteId = "site-001",
            DeviceId = deviceId,
            EdgeAgentId = edgeAgentId,
            CollectedAtUtc = DateTime.UtcNow,
            ReceivedAtUtc = DateTime.UtcNow,
            IngestedAtUtc = DateTime.UtcNow,
            TestCode = "TEST",
            PatientId = $"PAT-{counter}",
            OperatorId = "OP-001",
            NormalizedJson = "{}",
            RawPayloadJson = "{}"
        };
        queue.Add(result);
        counter++;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
});

Console.WriteLine("Edge agent started. Press Ctrl+C to exit.");

// Main upload loop. Takes results from the queue and attempts to post to the Cloud API.
while (true)
{
    Result? result = null;
    try
    {
        result = queue.Take();
    }
    catch (InvalidOperationException)
    {
        // Collection has been marked as complete for adding.
        break;
    }

    try
    {
        var response = await httpClient.PostAsJsonAsync("/api/results", result);
        if (!response.IsSuccessStatusCode)
        {
            // Requeue the result on failure for a later retry.
            queue.Add(result);
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
    catch
    {
        // Likely offline; requeue and back off.
        queue.Add(result);
        await Task.Delay(TimeSpan.FromSeconds(10));
    }
}