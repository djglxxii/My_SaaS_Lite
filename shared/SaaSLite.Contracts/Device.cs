using System;

namespace SaaSLite.Contracts
{
    /// <summary>
    /// Represents a device sending results. In the PoC we only track basic fields.
    /// </summary>
    public class Device
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public DateTime? LastSeenUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}