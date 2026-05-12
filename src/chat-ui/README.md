# Chat UI

Vue 3 frontend for the SignalR chat application.

## Structure

- `src/App.vue`: chat screen and local UI state.
- `src/services`: SignalR connection wrapper.
- `src/types`: chat DTOs shared conceptually with the backend contracts.

## Local Development

Start the backend first:

```bash
dotnet run --project ../ChatApi/ChatApi/ChatApi.csproj --launch-profile http
```

Then start the frontend:

```bash
pnpm dev
```

The app expects the chat hub at `http://localhost:5013/hubs/chat`. Override it with:

```bash
VITE_CHAT_HUB_URL=http://localhost:5013/hubs/chat pnpm dev
```

## Build

```bash
pnpm build
```
