# ChatApi

ASP.NET Core backend for the chat application.

## Structure

- `ChatApi/Program.cs`: application startup only.
- `ChatApi/Contracts`: request and response DTOs shared with clients.
- `ChatApi/Hubs`: SignalR hubs and strongly typed client contracts.
- `ChatApi/Services`: application services, currently in-memory connection tracking.
- `ChatApi/Extensions`: service registration and endpoint mapping.

## Local Development

Run the API:

```bash
dotnet run --project ChatApi/ChatApi.csproj
```

Available endpoints:

- `GET /health`
- `SignalR /hubs/chat`

The development CORS origin is configured for the Vue dev server at `http://localhost:5173`.

## Docker

Build the backend image from this folder:

```bash
docker build -t chat-api .
```

Run it on local port `5013`:

```bash
docker run --rm -p 5013:8080 chat-api
```
