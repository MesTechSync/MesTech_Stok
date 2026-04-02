using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Integration.Tests.Api;

/// <summary>
/// Custom WebApplicationFactory for MesTech.WebApi integration tests (E03).
/// Replaces PostgreSQL with InMemory EF Core and removes port-binding
/// background services to avoid conflicts during parallel test runs.
/// Uses environment variables for config that must be available during
/// service registration (before ConfigureServices overrides apply).
/// </summary>
public sealed class MesTechWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Test API key configured in appsettings override.</summary>
    public const string TestApiKey = "test-api-key-e03-integration";

    /// <summary>JWT secret (>= 32 chars) for test token generation.</summary>
    public const string TestJwtSecret = "TestJwtSecret_E03_IntegrationTests_Min32Chars!!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variables BEFORE host builds — these are needed during
        // ConfigureServices in Program.cs (MassTransitConfig reads them eagerly).
        Environment.SetEnvironmentVariable("RabbitMQ__Host", "localhost");
        Environment.SetEnvironmentVariable("RabbitMQ__Port", "5672");
        Environment.SetEnvironmentVariable("RabbitMQ__Username", "guest");
        Environment.SetEnvironmentVariable("RabbitMQ__Password", "guest");
        Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "mestech-test");
        Environment.SetEnvironmentVariable("Jwt__Audience", "mestech-test-clients");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
        // AES-256 requires exactly 32 bytes (256 bits) — base64 of 32 bytes
        Environment.SetEnvironmentVariable("Security__EncryptionKey", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        Environment.SetEnvironmentVariable("Mesa__UseProductionBridge", "false");
        Environment.SetEnvironmentVariable("Mesa__BridgeEnabled", "false");
        Environment.SetEnvironmentVariable("Mesa__Accounting__UseReal", "false");
        Environment.SetEnvironmentVariable("Mesa__Advisory__UseReal", "false");

        builder.UseEnvironment("Development");

        // Disable DI validation-on-build — some Dalga 10+ handlers reference
        // interfaces not yet registered in InfrastructureServiceRegistration.
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
        // Use InMemory marker (no "Host=" so Hangfire PostgreSQL is skipped)
        builder.UseSetting("ConnectionStrings:PostgreSQL", "InMemory=true");
        builder.UseSetting("ConnectionStrings:Redis", "localhost:3679");

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations to prevent dual-provider conflict.
            // EF Core registers multiple descriptors: DbContextOptions<T>, DbContextOptions,
            // AppDbContext, and IDbContextOptionsConfiguration<T>.
            var dbRelated = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().FullName?.Contains("IDbContextOptionsConfiguration") == true))
                .ToList();
            foreach (var descriptor in dbRelated)
                services.Remove(descriptor);

            // Add InMemory database for test isolation (single provider — no Npgsql conflict)
            var dbName = $"MesTech_Test_{Guid.NewGuid():N}";
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Remove port-binding BackgroundServices that conflict in tests
            RemoveHostedService(services, "HealthCheckEndpoint");
            RemoveHostedService(services, "MesaStatusEndpoint");
            RemoveHostedService(services, "RealtimeDashboardEndpoint");

            // Remove Redis StackExchange cache — replace with in-memory
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
        // Clean up environment variables
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
