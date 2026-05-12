namespace ChatApi.Contracts.Chat;

public sealed record UserPresenceResponse(
    string ConnectionId,
    string DisplayName,
    IReadOnlyCollection<string> Rooms,
    DateTimeOffset ConnectedAt);
