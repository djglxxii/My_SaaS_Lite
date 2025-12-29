using System;
using System.IO;
using System.Net.Http;
using SaaSLite.Contracts;
using SaaSLite.EdgeAgent.Queue;
using SaaSLite.EdgeAgent.Sim;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

static string GetEnv(string name, string @default) =>
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))
        ? @default
        : Environment.GetEnvironmentVariable(name)!.Trim();

var cloudBaseUrl = GetEnv("CLOUD_API_BASE_URL", "http://localhost:5000");
var siteId = GetEnv("SITE_ID", "site-001");
var deviceId = GetEnv("DEVICE_ID", "device-001");
var edgeAgentId = GetEnv("EDGE_AGENT_ID", "edge-001");

var queueRoot = GetEnv("QUEUE_DIR", Path.Combine(AppContext.BaseDirectory, "queue"));

var produceIntervalSeconds = int.TryParse(GetEnv("PRODUCE_INTERVAL_SECONDS", "3"), out var p) ? p : 3;
var heartbeatSeconds = int.TryParse(GetEnv("HEARTBEAT_SECONDS", "10"), out var h) ? h : 10;
var uploadIntervalMs = int.TryParse(GetEnv("UPLOAD_INTERVAL_MS", "1000"), out var u) ? u : 1000;
var uploadBatchSize = int.TryParse(GetEnv("UPLOAD_BATCH_SIZE", "10"), out var b) ? b : 10;

Console.WriteLine("SaaS Lite EdgeAgent PoC");
Console.WriteLine($"Cloud API: {cloudBaseUrl}");
Console.WriteLine($"SiteId: {siteId}  DeviceId: {deviceId}  EdgeAgentId: {edgeAgentId}");
Console.WriteLine($"QueueDir: {queueRoot}");

var queue = new FileBackedQueue(queueRoot);
var generator = new ResultGenerator(siteId, deviceId, edgeAgentId);

using var http = new HttpClient
{
    BaseAddress = new Uri(cloudBaseUrl.TrimEnd('/') + "/")
};

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var producerTask = RunProducerAsync(queue, generator, TimeSpan.FromSeconds(produceIntervalSeconds), cts.Token);
var uploaderTask = RunUploaderAsync(queue, http, TimeSpan.FromMilliseconds(uploadIntervalMs), uploadBatchSize, cts.Token);
var heartbeatTask = RunHeartbeatAsync(http, deviceId, siteId, TimeSpan.FromSeconds(heartbeatSeconds), cts.Token);

await Task.WhenAll(producerTask, uploaderTask, heartbeatTask);

static async Task RunProducerAsync(
    FileBackedQueue queue,
    ResultGenerator generator,
    TimeSpan interval,
    CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            var payload = generator.CreateNext();

            var env = new QueuedResultEnvelope
            {
                QueueItemId = Guid.NewGuid().ToString("N"),
                EnqueuedAtUtc = DateTime.UtcNow,
                Payload = payload
            };

            await queue.EnqueueAsync(env, ct);
            Console.WriteLine($"[PRODUCE] Enqueued ResultId={payload.ResultId} CollectedAtUtc={payload.CollectedAtUtc:O}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PRODUCE] ERROR: {ex.Message}");
        }

        await Task.Delay(interval, ct);
    }
}

static async Task RunUploaderAsync(
    FileBackedQueue queue,
    HttpClient http,
    TimeSpan interval,
    int batchSize,
    CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            var pending = queue.GetPendingFilesOldestFirst(batchSize);
            if (pending.Count == 0)
            {
                await Task.Delay(interval, ct);
                continue;
            }

            foreach (var file in pending)
            {
                ct.ThrowIfCancellationRequested();

                QueuedResultEnvelope? env = null;
                try
                {
                    env = await queue.ReadAsync(file, ct);
                    if (env?.Payload == null)
                    {
                        // Corrupt file; treat as "sent" to avoid infinite loop in PoC
                        Console.WriteLine($"[UPLOAD] WARN: corrupt queue file, moving to sent: {Path.GetFileName(file)}");
                        queue.MarkSent(file);
                        continue;
                    }

                    var resp = await http.PostAsJsonAsync("api/results", env.Payload, ct);
                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[UPLOAD] FAIL {resp.StatusCode} ResultId={env.Payload.ResultId} (will retry)");
                        break; // stop batch; try again later
                    }

                    queue.MarkSent(file);
                    Console.WriteLine($"[UPLOAD] OK ResultId={env.Payload.ResultId}");
                }
                catch (Exception ex)
                {
                    // Most common: API down. Stop processing and retry later.
                    var rid = env?.Payload?.ResultId ?? "(unknown)";
                    Console.WriteLine($"[UPLOAD] ERROR ResultId={rid}: {ex.Message} (will retry)");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UPLOAD] LOOP ERROR: {ex.Message}");
        }

        await Task.Delay(interval, ct);
    }
}

static async Task RunHeartbeatAsync(
    HttpClient http,
    string deviceId,
    string siteId,
    TimeSpan interval,
    CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            // For PoC, heartbeat is a simple POST with query params.
            var url = $"api/devices/{Uri.EscapeDataString(deviceId)}/heartbeat?siteId={Uri.EscapeDataString(siteId)}&deviceType=Simulated&displayName={Uri.EscapeDataString(deviceId)}";

            var resp = await http.PostAsync(url, content: null, ct);
            if (resp.IsSuccessStatusCode)
                Console.WriteLine($"[HEARTBEAT] OK {deviceId}");
            else
                Console.WriteLine($"[HEARTBEAT] FAIL {resp.StatusCode} {deviceId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HEARTBEAT] ERROR: {ex.Message}");
        }

        await Task.Delay(interval, ct);
    }
}
