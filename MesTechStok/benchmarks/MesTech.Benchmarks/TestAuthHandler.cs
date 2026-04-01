using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Benchmarks;

/// <summary>
/// Benchmark-only auth handler — JWT doğrulamasını bypass eder.
/// Tüm istekleri authenticated olarak işaretler ve tenant claim ekler.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tenantId = Request.Headers["X-Tenant-Id"].FirstOrDefault()
                    ?? Request.Query["tenantId"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "benchmark-user"),
            new Claim(ClaimTypes.Name, "Benchmark Runner"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("tenant_id", tenantId),
            new Claim("tenantId", tenantId),
            new Claim("sub", Guid.NewGuid().ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
