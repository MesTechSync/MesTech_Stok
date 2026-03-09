using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.Middleware;

/// <summary>
/// Extension methods for registering and using ApiKeyMiddleware.
/// Usage in DEV1 Web API startup:
///   services.AddApiKeyAuthentication(configuration);
///   app.UseApiKeyAuthentication();
/// </summary>
public static class ApiKeyMiddlewareExtensions
{
    /// <summary>Registers ApiKeyOptions from "ApiSecurity" config section.</summary>
    public static IServiceCollection AddApiKeyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ApiKeyOptions>(opt =>
            configuration.GetSection(ApiKeyOptions.Section).Bind(opt));
        return services;
    }

    /// <summary>Adds ApiKeyMiddleware to the request pipeline.</summary>
    public static IApplicationBuilder UseApiKeyAuthentication(
        this IApplicationBuilder app) =>
        app.UseMiddleware<ApiKeyMiddleware>();
}
