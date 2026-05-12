namespace ChatApi.Contracts.Chat;

public sealed record SendMessageRequest(string Message, string? Room = null);
