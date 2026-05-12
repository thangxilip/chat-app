# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Run from the solution root (`/Users/ThangVo/Documents/ThangVD/projects/chat-app/src/ChatApi`):

```bash
# Build
dotnet build

# Run (HTTP on port 5013)
dotnet run --project ChatApi/ChatApi.csproj

# Run (HTTPS on port 7290, HTTP on 5013)
dotnet run --project ChatApi/ChatApi.csproj --launch-profile https

# Watch mode
dotnet watch --project ChatApi/ChatApi.csproj

# Health check (once running)
curl http://localhost:5013/health
```

There are no tests in this project yet.

## Architecture

This is an ASP.NET Core 10 SignalR-based real-time chat API. The single project (`ChatApi`) uses a minimal-hosting model.

**Request flow:**
- Clients connect to `ws://localhost:5013/hubs/chat` via SignalR, optionally passing `?displayName=<name>` in the query string.
- On connect, each client is automatically placed into the `general` room (the only room users cannot leave).
- All SignalR hub methods (`SendMessage`, `JoinRoom`, `LeaveRoom`, `RenameUser`) live in `ChatHub.cs`.
- Connection state (rooms, display names, connect time) is tracked entirely in-memory by `InMemoryChatConnectionTracker`, registered as a singleton. **State is lost on restart.**

**Key design decisions:**
- `IChatConnectionTracker` is the only service abstraction — the hub delegates all state mutations to it.
- `IChatClient` (strongly-typed hub interface) defines the exact messages pushed to clients: `ReceiveMessage`, `ReceiveSystemMessage`, `UserJoined`, `UserLeft`, `ActiveUsersChanged`.
- CORS allowed origins are driven by config (`Cors:AllowedOrigins`); default allows `http://localhost:5173` (Vite dev server).
- OpenAPI (`/openapi/v1.json`) is only mapped in Development.
- All endpoint and service registrations are organized into `Extensions/` rather than directly in `Program.cs`.

**SignalR hub methods (client → server):**

| Method | Payload | Notes |
|---|---|---|
| `SendMessage` | `{ message, room? }` | Room defaults to `general`; caller must be in the room |
| `JoinRoom` | `{ room }` | Adds caller to a SignalR group |
| `LeaveRoom` | `{ room }` | Cannot leave `general` |
| `RenameUser` | `string displayName` | Broadcasts updated user list to all |

**SignalR events (server → client):**

| Event | Payload |
|---|---|
| `ReceiveMessage` | `ChatMessageResponse` |
| `ReceiveSystemMessage` | `string` |
| `UserJoined` / `UserLeft` | `UserPresenceResponse` |
| `ActiveUsersChanged` | `UserPresenceResponse[]` |
