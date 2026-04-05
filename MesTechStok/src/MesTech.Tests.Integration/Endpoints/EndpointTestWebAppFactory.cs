using System.Security.Claims;
using System.Text.Encodings.Web;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// WebApplicationFactory for endpoint hardening tests (Sprint 1 DEV-H1).
/// Uses InMemory EF Core, test auth handler, and stub health checks for test isolation.
/// Mirrors the pattern from tests/MesTech.Integration.Tests/Api/MesTechWebApplicationFactory.cs
/// </summary>
public sealed class EndpointTestWebAppFactory : WebApplicationFactory<Program>
{
    /// <summary>Test API key configured in appsettings override.</summary>
    public const string TestApiKey = "test-api-key-endpoint-hardening";

    /// <summary>JWT secret (>= 32 chars) for test token generation.</summary>
    public const string TestJwtSecret = "TestJwtSecret_EndpointHardening_Min32Chars!!";

    /// <summary>Authentication scheme used by test handler.</summary>
    public const string TestAuthScheme = "TestScheme";

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
        builder.UseSetting("ApiSecurity:BypassPaths:1", "/health/ready");
        builder.UseSetting("ApiSecurity:BypassPaths:2", "/metrics");
        builder.UseSetting("ApiSecurity:BypassPaths:3", "/api/v1/auth");
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

            // Remove ALL MassTransit registrations and re-add with InMemory transport
            // This avoids RabbitMQ connection during test startup
            var massTransitDescriptors = services.Where(d =>
                d.ServiceType.FullName?.Contains("MassTransit") == true ||
                d.ImplementationType?.FullName?.Contains("MassTransit") == true ||
                d.ImplementationFactory?.Method.ToString()?.Contains("MassTransit") == true ||
                (d.ServiceType == typeof(IHostedService) &&
                 (d.ImplementationType?.FullName?.Contains("MassTransit") == true ||
                  d.ImplementationType?.FullName?.Contains("BusHostedService") == true ||
                  d.ImplementationFactory?.Method.ToString()?.Contains("MassTransit") == true)))
                .ToList();
            foreach (var descriptor in massTransitDescriptors)
                services.Remove(descriptor);

            // Re-add MassTransit with InMemory transport (no RabbitMQ needed)
            services.AddMassTransit(x => x.UsingInMemory());

            // Remove Hangfire hosted services
            var hangfireDescriptors = services.Where(d =>
                d.ServiceType == typeof(IHostedService) &&
                (d.ImplementationType?.FullName?.Contains("Hangfire") == true ||
                 d.ImplementationFactory?.Method.ToString()?.Contains("Hangfire") == true))
                .ToList();
            foreach (var descriptor in hangfireDescriptors)
                services.Remove(descriptor);

            // Replace Redis with in-memory distributed cache
            var redisDescriptor = services.SingleOrDefault(
                d => d.ServiceType.FullName?.Contains("IDistributedCache") == true);
            if (redisDescriptor != null)
                services.Remove(redisDescriptor);
            services.AddDistributedMemoryCache();

            // ── Test Authentication: auto-authenticate when X-API-Key header is present ──
            // Override default auth with a test scheme that accepts API key as identity.
            services.AddAuthentication(TestAuthScheme)
                .AddScheme<AuthenticationSchemeOptions, TestApiKeyAuthHandler>(
                    TestAuthScheme, _ => { });

            // ── Stub Health Checks: remove infra checks that need PG/Redis/RabbitMQ ──
            var healthDescriptors = services.Where(d =>
                d.ServiceType.FullName?.Contains("IHealthCheck") == true)
                .ToList();
            foreach (var descriptor in healthDescriptors)
                services.Remove(descriptor);
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

/// <summary>
/// Test authentication handler that authenticates requests with a valid X-API-Key header.
/// Requests without the header are not authenticated (returns NoResult → 401).
/// </summary>
internal sealed class TestApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKey) ||
            string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (apiKey != EndpointTestWebAppFactory.TestApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "smoke-test-user"),
            new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000099"),
            new Claim("tenant_id", "00000000-0000-0000-0000-000000000001"),
        };
        var identity = new ClaimsIdentity(claims, EndpointTestWebAppFactory.TestAuthScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, EndpointTestWebAppFactory.TestAuthScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
