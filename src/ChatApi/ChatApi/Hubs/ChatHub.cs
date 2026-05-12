using ChatApi.Contracts.Chat;
using ChatApi.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Hubs;

public sealed class ChatHub(IChatConnectionTracker connectionTracker) : Hub<IChatClient>
{
    private const string DefaultRoom = "general";

    public override async Task OnConnectedAsync()
    {
        var displayName = GetDisplayName();
        var user = connectionTracker.Connect(Context.ConnectionId, displayName, DefaultRoom);

        await Groups.AddToGroupAsync(Context.ConnectionId, DefaultRoom);
        await Clients.Caller.ReceiveSystemMessage($"Connected to #{DefaultRoom} as {displayName}.");
        await Clients.Others.UserJoined(user);
        await BroadcastActiveUsersAsync();

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = connectionTracker.Disconnect(Context.ConnectionId);

        if (user is not null)
        {
            await Clients.Others.UserLeft(user);
            await BroadcastActiveUsersAsync();
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(SendMessageRequest request)
    {
        var message = Normalize(request.Message);
        var room = NormalizeRoom(request.Room);

        if (string.IsNullOrWhiteSpace(message))
        {
            await Clients.Caller.ReceiveSystemMessage("Message cannot be empty.");
            return;
        }

        if (!connectionTracker.IsInRoom(Context.ConnectionId, room))
        {
            await Clients.Caller.ReceiveSystemMessage($"Join #{room} before sending messages there.");
            return;
        }

        var sender = connectionTracker.GetUser(Context.ConnectionId);
        var response = new ChatMessageResponse(
            Guid.NewGuid().ToString("N"),
            Context.ConnectionId,
            sender?.DisplayName ?? GetDisplayName(),
            message,
            room,
            DateTimeOffset.UtcNow);

        await Clients.Group(room).ReceiveMessage(response);
    }

    public async Task JoinRoom(JoinRoomRequest request)
    {
        var room = NormalizeRoom(request.Room);

        if (string.IsNullOrWhiteSpace(room))
        {
            await Clients.Caller.ReceiveSystemMessage("Room name cannot be empty.");
            return;
        }

        connectionTracker.JoinRoom(Context.ConnectionId, room);
        await Groups.AddToGroupAsync(Context.ConnectionId, room);
        await Clients.Caller.ReceiveSystemMessage($"Joined #{room}.");
        await BroadcastActiveUsersAsync();
    }

    public async Task LeaveRoom(JoinRoomRequest request)
    {
        var room = NormalizeRoom(request.Room);

        if (room == DefaultRoom)
        {
            await Clients.Caller.ReceiveSystemMessage($"You cannot leave #{DefaultRoom}.");
            return;
        }

        connectionTracker.LeaveRoom(Context.ConnectionId, room);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
        await Clients.Caller.ReceiveSystemMessage($"Left #{room}.");
        await BroadcastActiveUsersAsync();
    }

    public async Task RenameUser(string displayName)
    {
        var normalizedDisplayName = Normalize(displayName);

        if (string.IsNullOrWhiteSpace(normalizedDisplayName))
        {
            await Clients.Caller.ReceiveSystemMessage("Display name cannot be empty.");
            return;
        }

        connectionTracker.Rename(Context.ConnectionId, normalizedDisplayName);
        await BroadcastActiveUsersAsync();
    }

    private Task BroadcastActiveUsersAsync()
    {
        return Clients.All.ActiveUsersChanged(connectionTracker.GetUsers());
    }

    private string GetDisplayName()
    {
        var requestedName = Context.GetHttpContext()?.Request.Query["displayName"].ToString();
        var normalizedName = Normalize(requestedName);

        return string.IsNullOrWhiteSpace(normalizedName)
            ? $"Guest-{Context.ConnectionId[..6]}"
            : normalizedName;
    }

    private static string NormalizeRoom(string? room)
    {
        var normalizedRoom = Normalize(room).ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalizedRoom) ? DefaultRoom : normalizedRoom;
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
