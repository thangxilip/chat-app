namespace ChatApi.Services;

public sealed record ChatUserConnection(
    string ConnectionId,
    string DisplayName,
    IReadOnlyCollection<string> Rooms,
    DateTimeOffset ConnectedAt);
