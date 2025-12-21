using System;

namespace SaaSLite.Contracts
{
    /// <summary>
    /// Canonical result entity used to ingest and transport test results.
    /// For the PoC most fields are simple strings or timestamps.
    /// </summary>
    public class Result
    {
        public string ResultId { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string EdgeAgentId { get; set; } = string.Empty;
        public DateTime CollectedAtUtc { get; set; }
        public DateTime ReceivedAtUtc { get; set; }
        public DateTime IngestedAtUtc { get; set; }
        public string TestCode { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string NormalizedJson { get; set; } = string.Empty;
        public string RawPayloadJson { get; set; } = string.Empty;
    }
}