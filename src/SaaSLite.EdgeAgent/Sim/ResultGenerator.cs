using System;
using System.Security.Cryptography;
using System.Text;
using SaaSLite.Contracts;

namespace SaaSLite.EdgeAgent.Sim;

public sealed class ResultGenerator
{
    private readonly string _siteId;
    private readonly string _deviceId;
    private readonly string _edgeAgentId;

    public ResultGenerator(string siteId, string deviceId, string edgeAgentId)
    {
        _siteId = siteId;
        _deviceId = deviceId;
        _edgeAgentId = edgeAgentId;
    }

    public ResultIngestRequest CreateNext()
    {
        var collectedAtUtc = DateTime.UtcNow;

        // Deterministic-ish for demo: hash a few fields to make a stable ResultId format.
        var seed = $"{_siteId}|{_deviceId}|{_edgeAgentId}|{collectedAtUtc:O}";
        var resultId = "r-" + Sha1Hex(seed).Substring(0, 12);

        // Simple demo payload
        var value = Random.Shared.Next(70, 140);

        return new ResultIngestRequest
        {
            ResultId = resultId,
            SiteId = _siteId,
            DeviceId = _deviceId,
            EdgeAgentId = _edgeAgentId,
            CollectedAtUtc = collectedAtUtc,

            TestCode = "GLU",
            PatientId = "p-" + Random.Shared.Next(100, 999),
            OperatorId = "op-" + Random.Shared.Next(1, 20),

            NormalizedJson = $"{{\"value\":{value},\"unit\":\"mg/dL\"}}",
            RawPayloadJson = $"{{\"raw\":\"SIM:{value}\"}}"
        };
    }

    private static string Sha1Hex(string s)
    {
        using var sha1 = SHA1.Create();
        var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}