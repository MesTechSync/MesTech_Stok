using System.Net.Http;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters.Hardening;

/// <summary>
/// PazaramaAdapter failure-mode hardening tests.
/// Pazarama uses OAuth 2.0 Client Credentials auth.
/// 6 scenarios: timeout, auth expired (401), rate limit (429),
/// server error (500), malformed JSON, network error.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Hardening", "FailureMode")]
public class PazaramaAdapterHardeningTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ILogger<PazaramaAdapter> _logger;

    private static readonly Dictionary<string, string> TestCredentials = new()
    {
        ["PazaramaClientId"] = "pz-hardening-client",
        ["PazaramaClientSecret"] = "pz-hardening-secret"
    };

    public PazaramaAdapterHardeningTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _server = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<PazaramaAdapter>();
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    private PazaramaAdapter CreateAdapter(TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        };
        return new PazaramaAdapter(httpClient, _logger);
    }

    private Dictionary<string, string> CredentialsWithBaseUrl() => new(TestCredentials)
    {
        ["BaseUrl"] = _fixture.BaseUrl
    };

    /// <summary>
    /// Configures WireMock to return a valid OAuth2 token.
    /// </summary>
    private void SetupOAuth2TokenEndpoint()
    {
        _server
            .Given(Request.Create()
                .WithPath("/connect/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""access_token"":""test-token"",""token_type"":""Bearer"",""expires_in"":3600}"));
    }

    // ────────────────────────────────────────────────────
    // Scenario 1: API Timeout
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_Timeout_GracefulFail()
    {
        // Arrange — all endpoints timeout (including OAuth token)
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(35)));

        var adapter = CreateAdapter(timeout: TimeSpan.FromSeconds(3));

        // Act
        var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());

        // Assert
        result.IsSuccess.Should().BeFalse("timeout should cause graceful failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 2: Auth Expired (401 from OAuth token endpoint)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_AuthExpired401_GracefulFail()
    {
        // Arrange — OAuth2 token endpoint returns 401
        _server
            .Given(Request.Create()
                .WithPath("/connect/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""invalid_client""}"));

        // Also handle brand endpoint in case token acquisition is skipped
        _server
            .Given(Request.Create()
                .WithPath("/brand/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody(@"{""error"":""Unauthorized""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());

        // Assert
        result.IsSuccess.Should().BeFalse("401 auth failure should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 3: Rate Limit (429)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_RateLimit429_GracefulFail()
    {
        // Arrange — OAuth works, brand endpoint returns 429
        SetupOAuth2TokenEndpoint();

        _server
            .Given(Request.Create()
                .WithPath("/brand/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "60")
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""message"":""Rate limit exceeded""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());

        // Assert
        result.IsSuccess.Should().BeFalse("429 rate limit should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 4: Server Error (500)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_ServerError500_GracefulFail()
    {
        // Arrange — OAuth works, brand endpoint returns 500
        SetupOAuth2TokenEndpoint();

        _server
            .Given(Request.Create()
                .WithPath("/brand/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""message"":""Internal Server Error""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());

        // Assert
        result.IsSuccess.Should().BeFalse("500 should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 5: Malformed JSON Response
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_MalformedJson_GracefulFail()
    {
        // Arrange — OAuth token endpoint returns malformed JSON
        _server
            .Given(Request.Create()
                .WithPath("/connect/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("this is not valid json {{{"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());

        // Assert
        result.IsSuccess.Should().BeFalse("malformed JSON should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 6: Network Error (server unreachable)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_NetworkError_GracefulFail()
    {
        // Arrange — point to a non-existent server port
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:1"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        var adapter = new PazaramaAdapter(httpClient, _logger);
        var creds = new Dictionary<string, string>
        {
            ["PazaramaClientId"] = "client-id",
            ["PazaramaClientSecret"] = "client-secret",
            ["BaseUrl"] = "http://127.0.0.1:1"
        };

        // Act
        var result = await adapter.TestConnectionAsync(creds);

        // Assert
        result.IsSuccess.Should().BeFalse("network error should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
