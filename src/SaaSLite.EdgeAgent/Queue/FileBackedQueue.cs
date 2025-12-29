using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SaaSLite.EdgeAgent.Queue;

public sealed class FileBackedQueue
{
    private readonly string _pendingDir;
    private readonly string _sentDir;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileBackedQueue(string rootDir)
    {
        _pendingDir = Path.Combine(rootDir, "pending");
        _sentDir = Path.Combine(rootDir, "sent");

        Directory.CreateDirectory(_pendingDir);
        Directory.CreateDirectory(_sentDir);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task EnqueueAsync(QueuedResultEnvelope env, CancellationToken ct)
    {
        if (env is null) throw new ArgumentNullException(nameof(env));
        if (env.Payload is null) throw new ArgumentException("Payload cannot be null.", nameof(env));

        // File name sortable by time; include ResultId for debugging.
        var safeResultId = MakeFileSafe(env.Payload.ResultId);
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeResultId}_{Guid.NewGuid():N}.json";
        var fullPath = Path.Combine(_pendingDir, fileName);

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(fs, env, _jsonOptions, ct);
        }
    }

    public IReadOnlyList<string> GetPendingFilesOldestFirst(int maxCount)
    {
        var files = Directory.GetFiles(_pendingDir, "*.json", SearchOption.TopDirectoryOnly);
        return files
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Take(maxCount)
            .ToList();
    }

    public async Task<QueuedResultEnvelope?> ReadAsync(string pendingFilePath, CancellationToken ct)
    {
        await using var fs = new FileStream(pendingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await JsonSerializer.DeserializeAsync<QueuedResultEnvelope>(fs, _jsonOptions, ct);
    }

    public void MarkSent(string pendingFilePath)
    {
        var fileName = Path.GetFileName(pendingFilePath);
        var dest = Path.Combine(_sentDir, fileName);

        // Overwrite is fine for PoC; should not happen normally.
        if (File.Exists(dest))
            File.Delete(dest);

        File.Move(pendingFilePath, dest);
    }

    private static string MakeFileSafe(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "no_result_id";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}