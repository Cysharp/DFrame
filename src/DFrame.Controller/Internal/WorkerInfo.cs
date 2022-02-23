using DFrame.Controller;

namespace DFrame;

internal record WorkerInfo(
    WorkerId WorkerId,
    Guid ConnectionId,
    DateTime ConnectTime,
    IReadOnlyList<KeyValuePair<string, string>> Metadata
);