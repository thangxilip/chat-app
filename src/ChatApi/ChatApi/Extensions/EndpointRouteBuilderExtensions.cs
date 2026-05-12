using ChatApi.Hubs;

namespace ChatApi.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            CheckedAt = DateTimeOffset.UtcNow
        }));

        endpoints.MapHub<ChatHub>("/hubs/chat");

        return endpoints;
    }
}
