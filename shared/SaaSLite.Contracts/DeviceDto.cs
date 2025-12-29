using System;

namespace SaaSLite.Contracts;

public sealed class DeviceDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;

    public DateTime LastSeenUtc { get; set; }

    // Computed by API (based on LastSeenUtc)
    public string Status { get; set; } = "Unknown";
}