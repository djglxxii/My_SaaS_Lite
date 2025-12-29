using System;

namespace SaaSLite.CloudApi.Data.Entities;

public sealed class ResultEntity
{
    // Internal surrogate key for EF/SQLite
    public long Id { get; set; }

    // External idempotency key from edge
    public string ResultId { get; set; } = string.Empty;

    public string SiteId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string EdgeAgentId { get; set; } = string.Empty;

    public DateTime CollectedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }

    public string TestCode { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;

    // PoC: store JSON blobs as text
    public string NormalizedJson { get; set; } = "{}";
    public string RawPayloadJson { get; set; } = "{}";
}
