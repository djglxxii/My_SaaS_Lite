using System;
using SaaSLite.Contracts;

namespace SaaSLite.EdgeAgent.Queue;

public sealed class QueuedResultEnvelope
{
    public string QueueItemId { get; set; } = string.Empty;
    public DateTime EnqueuedAtUtc { get; set; }

    public ResultIngestRequest Payload { get; set; } = new ResultIngestRequest();
}
