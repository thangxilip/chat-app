# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Full-stack real-time chat application using **Vue 3 + TypeScript** frontend and **ASP.NET Core 10 + SignalR** backend. No database — all state is in-memory and ephemeral.

Repository layout:
```
chat-app/src/
├── chat-ui/          # Vue 3 frontend (this working directory)
└── ../ChatApi/       # ASP.NET Core backend
    └── ChatApi/
```

## Frontend Commands (chat-ui)

Package manager: **pnpm**

```bash
pnpm dev        # Dev server on http://localhost:5173
pnpm build      # vue-tsc type check + vite build → dist/
pnpm lint       # oxlint + eslint --fix
pnpm format     # prettier --write
```

Environment variable required for dev:
```bash
VITE_CHAT_HUB_URL=http://localhost:5013/hubs/chat pnpm dev
```

Copy `.env.example` → `.env.local` to persist this.

## Backend Commands (ChatApi)

```bash
dotnet run --project ChatApi/ChatApi/ChatApi.csproj --launch-profile http
# Starts on http://localhost:5013
# Health check: GET /health
# SignalR hub: /hubs/chat

docker build -t chat-api .   # Multi-stage build, exposes port 8080
```

## Architecture

### Communication Flow

1. Frontend connects to `VITE_CHAT_HUB_URL` via SignalR, passing `displayName` as a query param.
2. `ChatHub` registers the connection in `InMemoryChatConnectionTracker` (thread-safe dictionary with `Lock`).
3. All new connections auto-join the `general` room.
4. Messages flow through hub methods: client calls `SendMessage` → hub broadcasts `ReceiveMessage` to the room group.
5. Room membership changes trigger `ActiveUsersChanged` broadcast to keep presence in sync.
6. On disconnect, the tracker cleans up and broadcasts `UserLeft`/`ActiveUsersChanged`.

### Frontend Structure

- `src/App.vue` — entire UI and reactive state in a single SFC (no Pinia/Vuex)
- `src/services/chatConnection.ts` — `ChatConnection` class wrapping SignalR; registers all hub event handlers
- `src/types/chat.ts` — TypeScript interfaces mirroring backend DTOs

### Backend Structure

- `Program.cs` — service registration + minimal API endpoint mapping
- `Hubs/ChatHub.cs` — all real-time logic (send, join, leave, rename)
- `Hubs/IChatClient.cs` — strongly-typed client interface (enforces hub method names)
- `Services/InMemoryChatConnectionTracker.cs` — thread-safe connection registry
- `Contracts/Chat/` — DTOs shared with frontend as TypeScript types

### SignalR Hub Methods

| Direction | Method |
|-----------|--------|
| Client → Server | `SendMessage`, `JoinRoom`, `LeaveRoom`, `RenameUser` |
| Server → Client | `ReceiveMessage`, `ReceiveSystemMessage`, `UserJoined`, `UserLeft`, `ActiveUsersChanged` |

## Code Style

Prettier config (`.prettierrc.json`): `semi: false`, `singleQuote: true`, `printWidth: 100`.

Node version constraint: `^20.19.0 || >=22.12.0`.

Path alias `@` maps to `src/` (configured in `vite.config.ts` and `tsconfig`).
