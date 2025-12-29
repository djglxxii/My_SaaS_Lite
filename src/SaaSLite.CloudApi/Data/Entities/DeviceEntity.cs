using System;

namespace SaaSLite.CloudApi.Data.Entities;

public sealed class DeviceEntity
{
    // Natural key
    public string DeviceId { get; set; } = string.Empty;

    public string SiteId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;

    public DateTime LastSeenUtc { get; set; }
}
