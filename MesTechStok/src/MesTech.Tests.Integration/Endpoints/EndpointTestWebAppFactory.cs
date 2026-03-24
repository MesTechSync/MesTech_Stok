using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// WebApplicationFactory for endpoint hardening tests (Sprint 1 DEV-H1).
/// Uses InMemory EF Core and mocked external services for test isolation.
/// Mirrors the pattern from tests/MesTech.Integration.Tests/Api/MesTechWebApplicationFactory.cs
/// </summary>
public sealed class EndpointTestWebAppFactory : WebApplicationFactory<Program>
{
    /// <summary>Test API key configured in appsettings override.</summary>
    public const string TestApiKey = "test-api-key-endpoint-hardening";

    /// <summary>JWT secret (>= 32 chars) for test token generation.</summary>
    public const string TestJwtSecret = "TestJwtSecret_EndpointHardening_Min32Chars!!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variables BEFORE host builds
        Environment.SetEnvironmentVariable("RabbitMQ__Host", "localhost");
        Environment.SetEnvironmentVariable("RabbitMQ__Port", "5672");
        Environment.SetEnvironmentVariable("RabbitMQ__Username", "guest");
        Environment.SetEnvironmentVariable("RabbitMQ__Password", "guest");
        Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "mestech-test");
        Environment.SetEnvironmentVariable("Jwt__Audience", "mestech-test-clients");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Security__EncryptionKey", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        Environment.SetEnvironmentVariable("Mesa__UseProductionBridge", "false");
        Environment.SetEnvironmentVariable("Mesa__BridgeEnabled", "false");
        Environment.SetEnvironmentVariable("Mesa__Accounting__UseReal", "false");
        Environment.SetEnvironmentVariable("Mesa__Advisory__UseReal", "false");

        builder.UseEnvironment("Development");

        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = false;
            options.ValidateScopes = false;
        });

        // UseSetting injects values before Program.cs reads them
        builder.UseSetting("ApiSecurity:ValidApiKeys:0", TestApiKey);
        builder.UseSetting("ApiSecurity:HeaderName", "X-API-Key");
        builder.UseSetting("ApiSecurity:BypassPaths:0", "/health");
        builder.UseSetting("ApiSecurity:BypassPaths:1", "/metrics");
        builder.UseSetting("ApiSecurity:BypassPaths:2", "/api/v1/auth");
        builder.UseSetting("ConnectionStrings:PostgreSQL", "InMemory=true");
        builder.UseSetting("ConnectionStrings:Redis", "localhost:3679");

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations
            var dbRelated = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().FullName?.Contains("IDbContextOptionsConfiguration") == true))
                .ToList();
            foreach (var descriptor in dbRelated)
                services.Remove(descriptor);

            // InMemory database for test isolation
            var dbName = $"MesTech_EndpointTest_{Guid.NewGuid():N}";
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Remove port-binding BackgroundServices
            RemoveHostedService(services, "HealthCheckEndpoint");
            RemoveHostedService(services, "MesaStatusEndpoint");
            RemoveHostedService(services, "RealtimeDashboardEndpoint");

            // Replace Redis with in-memory distributed cache
            var redisDescriptor = services.SingleOrDefault(
                d => d.ServiceType.FullName?.Contains("IDistributedCache") == true);
            if (redisDescriptor != null)
                services.Remove(redisDescriptor);
            services.AddDistributedMemoryCache();
        });
    }

    private static void RemoveHostedService(IServiceCollection services, string typeName)
    {
        var descriptors = services.Where(d =>
            d.ServiceType == typeof(IHostedService) &&
            (d.ImplementationType?.Name == typeName ||
             d.ImplementationFactory?.Method.ToString()?.Contains(typeName) == true))
            .ToList();

        foreach (var descriptor in descriptors)
            services.Remove(descriptor);
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable("RabbitMQ__Host", null);
        Environment.SetEnvironmentVariable("RabbitMQ__Port", null);
        Environment.SetEnvironmentVariable("RabbitMQ__Username", null);
        Environment.SetEnvironmentVariable("RabbitMQ__Password", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", null);
        Environment.SetEnvironmentVariable("Security__EncryptionKey", null);
        Environment.SetEnvironmentVariable("Mesa__UseProductionBridge", null);
        Environment.SetEnvironmentVariable("Mesa__BridgeEnabled", null);
        Environment.SetEnvironmentVariable("Mesa__Accounting__UseReal", null);
        Environment.SetEnvironmentVariable("Mesa__Advisory__UseReal", null);

        base.Dispose(disposing);
    }
}
