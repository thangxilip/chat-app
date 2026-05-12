using ChatApi.Contracts.Chat;

namespace ChatApi.Services;

public interface IChatConnectionTracker
{
    UserPresenceResponse Connect(string connectionId, string displayName, string defaultRoom);

    UserPresenceResponse? Disconnect(string connectionId);

    ChatUserConnection? GetUser(string connectionId);

    IReadOnlyCollection<UserPresenceResponse> GetUsers();

    bool IsInRoom(string connectionId, string room);

    void JoinRoom(string connectionId, string room);

    void LeaveRoom(string connectionId, string room);

    void Rename(string connectionId, string displayName);
}
