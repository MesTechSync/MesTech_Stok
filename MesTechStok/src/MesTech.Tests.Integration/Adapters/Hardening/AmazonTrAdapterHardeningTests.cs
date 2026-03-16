using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters.Hardening;

/// <summary>
/// AmazonTrAdapter failure-mode hardening tests.
/// Amazon uses LWA OAuth2 token exchange before API calls.
/// 6 scenarios: timeout, auth expired (401), rate limit (429),
/// server error (500), malformed JSON, network error.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Hardening", "FailureMode")]
public class AmazonTrAdapterHardeningTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ILogger<AmazonTrAdapter> _logger;

    private static readonly Dictionary<string, string> TestCredentials = new()
    {
        ["RefreshToken"] = "amz-test-refresh-token",
        ["ClientId"] = "amz-test-client-id",
        ["ClientSecret"] = "amz-test-client-secret",
        ["SellerId"] = "AMZ-SELLER-001"
    };

    public AmazonTrAdapterHardeningTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _server = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<AmazonTrAdapter>();
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    private AmazonTrAdapter CreateAdapter(TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        };
        return new AmazonTrAdapter(httpClient, _logger);
    }

    private Dictionary<string, string> CredentialsWithBaseUrl() => new(TestCredentials)
    {
        ["BaseUrl"] = _fixture.BaseUrl,
        ["LwaEndpoint"] = $"{_fixture.BaseUrl}/auth/o2/token"
    };

    /// <summary>
    /// Configures WireMock to return a valid LWA token so we can test subsequent API calls.
    /// </summary>
    private void SetupLwaTokenEndpoint()
    {
        _server
            .Given(Request.Create()
                .WithPath("/auth/o2/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""access_token"":""test-access-token"",""token_type"":""bearer"",""expires_in"":3600}"));
    }

    // ────────────────────────────────────────────────────
    // Scenario 1: API Timeout
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_Timeout_GracefulFail()
    {
        // Arrange — all requests timeout (including LWA token)
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
    // Scenario 2: Auth Expired (401 from LWA)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_AuthExpired401_GracefulFail()
    {
        // Arrange — LWA token endpoint returns 401
        _server
            .Given(Request.Create()
                .WithPath("/auth/o2/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""invalid_client"",""error_description"":""Client authentication failed""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());

        // Assert
        result.IsSuccess.Should().BeFalse("401 from LWA should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 3: Rate Limit (429)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_RateLimit429_GracefulFail()
    {
        // Arrange — LWA works, but catalog endpoint returns 429
        SetupLwaTokenEndpoint();

        _server
            .Given(Request.Create()
                .WithPath("/catalog/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "5")
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":[{""code"":""QuotaExceeded"",""message"":""You exceeded your quota""}]}"));

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
        // Arrange — LWA works, catalog returns 500
        SetupLwaTokenEndpoint();

        _server
            .Given(Request.Create()
                .WithPath("/catalog/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""errors"":[{""code"":""InternalFailure"",""message"":""Internal service error""}]}"));

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
        // Arrange — LWA returns malformed JSON
        _server
            .Given(Request.Create()
                .WithPath("/auth/o2/token")
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
        var adapter = new AmazonTrAdapter(httpClient, _logger);
        var creds = new Dictionary<string, string>
        {
            ["RefreshToken"] = "token",
            ["ClientId"] = "id",
            ["ClientSecret"] = "secret",
            ["SellerId"] = "seller",
            ["BaseUrl"] = "http://127.0.0.1:1",
            ["LwaEndpoint"] = "http://127.0.0.1:1/auth/o2/token"
        };

        // Act
        var result = await adapter.TestConnectionAsync(creds);

        // Assert
        result.IsSuccess.Should().BeFalse("network error should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
