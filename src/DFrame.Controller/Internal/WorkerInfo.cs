namespace DFrame;

internal record WorkerInfo(
    WorkerId WorkerId,
    Guid ConnectionId,
    DateTime ConnectTime,
    IReadOnlyList<(string, string)> Metadata
);