using DFrame.Controller;

namespace DFrame;

internal record WorkerInfo(
    WorkerId WorkerId,
    Guid ConnectionId,
    DateTime ConnectTime,
    Dictionary<string, string> Metadata
);