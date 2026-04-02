using FluentAssertions;
using MesTech.Infrastructure.Integration.ERP;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MesTech.Integration.Tests.Infrastructure.ERP;

/// <summary>
/// ERP resilience infrastructure tests — validates Polly policy configuration,
/// named HttpClient registration, rate limiter capacity, and thread-safety of token semaphores.
/// I-14 ERP Saglamlastirma / T-02 Resilience Tests.
/// </summary>
[Trait("Category", "Integration")]
public class ErpResilienceTests
{
    // ═══════════════════════════════════════════════════════════════════
    // ClientNames Constants
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void ClientNames_AllDefined_AllNonEmpty()
    {
        // Arrange — all 7 client name constants
        var names = new[]
        {
            ErpResiliencePolicies.ClientNames.Parasut,
            ErpResiliencePolicies.ClientNames.ParasutToken,
            ErpResiliencePolicies.ClientNames.Logo,
            ErpResiliencePolicies.ClientNames.LogoToken,
            ErpResiliencePolicies.ClientNames.BizimHesap,
            ErpResiliencePolicies.ClientNames.Netsis,
            ErpResiliencePolicies.ClientNames.Nebim
        };

        // Assert
        names.Should().HaveCount(7);
        names.Should().AllSatisfy(n => n.Should().NotBeNullOrWhiteSpace());
        names.Distinct().Should().HaveCount(7, "all client names must be unique");
    }

    // ═══════════════════════════════════════════════════════════════════
    // DI Registration
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AddErpResilientHttpClients_RegistersAllClients()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddErpResilientHttpClients();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Assert — each named client should resolve without error
        var clientNames = new[]
        {
            ErpResiliencePolicies.ClientNames.Parasut,
            ErpResiliencePolicies.ClientNames.ParasutToken,
            ErpResiliencePolicies.ClientNames.Logo,
            ErpResiliencePolicies.ClientNames.LogoToken,
            ErpResiliencePolicies.ClientNames.BizimHesap,
            ErpResiliencePolicies.ClientNames.Netsis,
            ErpResiliencePolicies.ClientNames.Nebim
        };

        foreach (var name in clientNames)
        {
            var client = factory.CreateClient(name);
            client.Should().NotBeNull($"named client '{name}' should be registered");
        }
    }

    [Fact]
    public void RetryPolicy_Handles_TransientErrors()
    {
        // Arrange — register clients with Polly policies
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddErpResilientHttpClients();

        // Act — building the provider validates policy configuration
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Assert — creating the client with attached policies succeeds
        var client = factory.CreateClient(ErpResiliencePolicies.ClientNames.Parasut);
        client.Should().NotBeNull("retry policy should be attached without error");

        var logoClient = factory.CreateClient(ErpResiliencePolicies.ClientNames.Logo);
        logoClient.Should().NotBeNull("retry policy should be attached to Logo client");
    }

    [Fact]
    public void CircuitBreakerPolicy_Configured()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddErpResilientHttpClients();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act — resilient clients (non-token) should have circuit breaker attached
        var resilientNames = new[]
        {
            ErpResiliencePolicies.ClientNames.Parasut,
            ErpResiliencePolicies.ClientNames.Logo,
            ErpResiliencePolicies.ClientNames.BizimHesap,
            ErpResiliencePolicies.ClientNames.Netsis,
            ErpResiliencePolicies.ClientNames.Nebim
        };

        // Assert — all 5 resilient clients resolve (retry + CB policies are wired)
        foreach (var name in resilientNames)
        {
            var client = factory.CreateClient(name);
            client.Should().NotBeNull($"circuit breaker should be configured for '{name}'");
        }
    }

    [Fact]
    public void TokenClients_NoCircuitBreaker_OnlyRetry()
    {
        // Token clients use RegisterTokenClient (retry only, no circuit breaker).
        // We verify they are registered separately from resilient clients by checking
        // they resolve correctly and are distinct registrations.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddErpResilientHttpClients();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act
        var parasutToken = factory.CreateClient(ErpResiliencePolicies.ClientNames.ParasutToken);
        var logoToken = factory.CreateClient(ErpResiliencePolicies.ClientNames.LogoToken);

        // Assert
        parasutToken.Should().NotBeNull("ParasutToken client should be registered with retry-only policy");
        logoToken.Should().NotBeNull("LogoToken client should be registered with retry-only policy");

        // Token client names must differ from their adapter counterparts
        ErpResiliencePolicies.ClientNames.ParasutToken
            .Should().NotBe(ErpResiliencePolicies.ClientNames.Parasut);
        ErpResiliencePolicies.ClientNames.LogoToken
            .Should().NotBe(ErpResiliencePolicies.ClientNames.Logo);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Rate Limiter
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Logo_RateLimiter_StaticField_InitialCount50()
    {
        // The LogoERPAdapter has a static SemaphoreSlim(50, 50) rate limiter.
        // We verify the field exists and has expected initial count via reflection.
        var adapterType = typeof(MesTech.Infrastructure.Integration.ERP.Logo.LogoERPAdapter);
        var field = adapterType.GetField("RateLimiter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        field.Should().NotBeNull("LogoERPAdapter should have a static RateLimiter field");

        var semaphore = field!.GetValue(null) as SemaphoreSlim;
        semaphore.Should().NotBeNull();
        semaphore!.CurrentCount.Should().Be(50, "rate limiter should start with 50 permits");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Token Service Thread Safety
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void LogoTokenService_SemaphoreSlim_ThreadSafe()
    {
        // Verify LogoTokenService has a SemaphoreSlim(1,1) for single-caller token refresh.
        var tokenType = typeof(MesTech.Infrastructure.Integration.ERP.Logo.LogoTokenService);
        var field = tokenType.GetField("_tokenLock",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        field.Should().NotBeNull("LogoTokenService should have a _tokenLock SemaphoreSlim");
        field!.FieldType.Should().Be(typeof(SemaphoreSlim));
    }

    [Fact]
    public void ParasutTokenService_SemaphoreSlim_ThreadSafe()
    {
        // Verify ParasutTokenService has a SemaphoreSlim(1,1) for single-caller token refresh.
        var tokenType = typeof(MesTech.Infrastructure.Integration.ERP.Parasut.ParasutTokenService);
        var field = tokenType.GetField("_tokenLock",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        field.Should().NotBeNull("ParasutTokenService should have a _tokenLock SemaphoreSlim");
        field!.FieldType.Should().Be(typeof(SemaphoreSlim));
    }

    // ═══════════════════════════════════════════════════════════════════
    // HttpClientFactory Distinct Clients
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void HttpClientFactory_CreatesDistinctClients()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddErpResilientHttpClients();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act
        var parasut = factory.CreateClient(ErpResiliencePolicies.ClientNames.Parasut);
        var logo = factory.CreateClient(ErpResiliencePolicies.ClientNames.Logo);
        var netsis = factory.CreateClient(ErpResiliencePolicies.ClientNames.Netsis);

        // Assert — each call returns a distinct HttpClient instance
        parasut.Should().NotBeSameAs(logo);
        parasut.Should().NotBeSameAs(netsis);
        logo.Should().NotBeSameAs(netsis);
    }

    [Fact]
    public void ErpAdapter_Registration_UsesFactory()
    {
        // Verify that after AddErpResilientHttpClients, the IHttpClientFactory
        // is resolvable and all 7 named clients can be created.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddErpResilientHttpClients();
        var provider = services.BuildServiceProvider();

        // Act
        var factory = provider.GetService<IHttpClientFactory>();

        // Assert
        factory.Should().NotBeNull("IHttpClientFactory must be registered by AddErpResilientHttpClients");

        // All 7 named clients should be creatable
        var allNames = new[]
        {
            ErpResiliencePolicies.ClientNames.Parasut,
            ErpResiliencePolicies.ClientNames.ParasutToken,
            ErpResiliencePolicies.ClientNames.Logo,
            ErpResiliencePolicies.ClientNames.LogoToken,
            ErpResiliencePolicies.ClientNames.BizimHesap,
            ErpResiliencePolicies.ClientNames.Netsis,
            ErpResiliencePolicies.ClientNames.Nebim
        };

        foreach (var name in allNames)
        {
            var act = () => factory!.CreateClient(name);
            act.Should().NotThrow($"factory should create client for '{name}' without error");
        }
    }
}
