namespace DFrame;

internal record WorkerInfo(
    WorkerId WorkerId,
    Guid ConnectionId,
    DateTime ConnectTime,
    IReadOnlyDictionary<string, string> Metadata
);