namespace ChatApi.Contracts.Chat;

public sealed record ChatMessageResponse(
    string Id,
    string SenderConnectionId,
    string SenderDisplayName,
    string Message,
    string Room,
    DateTimeOffset SentAt);
