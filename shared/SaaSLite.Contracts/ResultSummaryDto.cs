using System;

namespace SaaSLite.Contracts;

public sealed class ResultSummaryDto
{
    public string ResultId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;

    public DateTime CollectedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }

    public string TestCode { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
}
