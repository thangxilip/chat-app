using ChatApi.Contracts.Chat;

namespace ChatApi.Services;

public sealed class InMemoryChatConnectionTracker : IChatConnectionTracker
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, TrackedConnection> _connections = [];

    public UserPresenceResponse Connect(string connectionId, string displayName, string defaultRoom)
    {
        var connection = new TrackedConnection(
            connectionId,
            displayName,
            [defaultRoom],
            DateTimeOffset.UtcNow);

        lock (_lock)
        {
            _connections[connectionId] = connection;
        }

        return ToPresenceResponse(connection);
    }

    public UserPresenceResponse? Disconnect(string connectionId)
    {
        lock (_lock)
        {
            if (!_connections.Remove(connectionId, out var connection))
            {
                return null;
            }

            return ToPresenceResponse(connection);
        }
    }

    public ChatUserConnection? GetUser(string connectionId)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(connectionId, out var connection)
                ? ToUserConnection(connection)
                : null;
        }
    }

    public IReadOnlyCollection<UserPresenceResponse> GetUsers()
    {
        lock (_lock)
        {
            return _connections.Values
                .OrderBy(connection => connection.DisplayName)
                .ThenBy(connection => connection.ConnectedAt)
                .Select(ToPresenceResponse)
                .ToArray();
        }
    }

    public bool IsInRoom(string connectionId, string room)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(connectionId, out var connection)
                && connection.Rooms.Contains(room);
        }
    }

    public void JoinRoom(string connectionId, string room)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
            {
                return;
            }

            connection.Rooms.Add(room);
        }
    }

    public void LeaveRoom(string connectionId, string room)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                connection.Rooms.Remove(room);
            }
        }
    }

    public void Rename(string connectionId, string displayName)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                connection.DisplayName = displayName;
            }
        }
    }

    private static UserPresenceResponse ToPresenceResponse(TrackedConnection connection)
    {
        return new UserPresenceResponse(
            connection.ConnectionId,
            connection.DisplayName,
            connection.Rooms.Order().ToArray(),
            connection.ConnectedAt);
    }

    private static ChatUserConnection ToUserConnection(TrackedConnection connection)
    {
        return new ChatUserConnection(
            connection.ConnectionId,
            connection.DisplayName,
            connection.Rooms.Order().ToArray(),
            connection.ConnectedAt);
    }

    private sealed class TrackedConnection(
        string connectionId,
        string displayName,
        IEnumerable<string> rooms,
        DateTimeOffset connectedAt)
    {
        public string ConnectionId { get; } = connectionId;

        public string DisplayName { get; set; } = displayName;

        public HashSet<string> Rooms { get; } = new(rooms, StringComparer.OrdinalIgnoreCase);

        public DateTimeOffset ConnectedAt { get; } = connectedAt;
    }
}
