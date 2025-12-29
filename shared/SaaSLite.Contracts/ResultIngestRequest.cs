using System;

namespace SaaSLite.Contracts;

public sealed class ResultIngestRequest
{
    public string ResultId { get; set; } = string.Empty;      // idempotency key
    public string SiteId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string EdgeAgentId { get; set; } = string.Empty;

    public DateTime CollectedAtUtc { get; set; }

    public string TestCode { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;

    // PoC: accept JSON blobs as strings (can be empty)
    public string NormalizedJson { get; set; } = "{}";
    public string RawPayloadJson { get; set; } = "{}";
}