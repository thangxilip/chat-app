using ChatApi.Services;

namespace ChatApi.Extensions;

public static class ServiceCollectionExtensions
{
    public const string ClientCorsPolicy = "ClientCors";

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddSignalR();
        services.AddSingleton<IChatConnectionTracker, InMemoryChatConnectionTracker>();

        services.AddCors(options =>
        {
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            options.AddPolicy(ClientCorsPolicy, policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
