using ChatApi.Contracts.Chat;

namespace ChatApi.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(ChatMessageResponse message);

    Task ReceiveSystemMessage(string message);

    Task UserJoined(UserPresenceResponse user);

    Task UserLeft(UserPresenceResponse user);

    Task ActiveUsersChanged(IReadOnlyCollection<UserPresenceResponse> users);
}
