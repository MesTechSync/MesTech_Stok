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
/// OpenCartAdapter failure-mode hardening tests.
/// 6 scenarios: timeout, auth expired (401), rate limit (429),
/// server error (500), malformed JSON, network error.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Hardening", "FailureMode")]
public class OpenCartAdapterHardeningTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ILogger<OpenCartAdapter> _logger;

    private static readonly Dictionary<string, string> TestCredentials = new()
    {
        ["ApiToken"] = "oc-hardening-token",
        ["BaseUrl"] = "placeholder"
    };

    public OpenCartAdapterHardeningTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _server = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<OpenCartAdapter>();
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    private OpenCartAdapter CreateAdapter(TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        };
        return new OpenCartAdapter(httpClient, _logger);
    }

    private Dictionary<string, string> CredentialsWithBaseUrl() => new()
    {
        ["ApiToken"] = "oc-hardening-token",
        ["BaseUrl"] = _fixture.BaseUrl
    };

    // ────────────────────────────────────────────────────
    // Scenario 1: API Timeout
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_Timeout_GracefulFail()
    {
        // Arrange
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
    // Scenario 2: Auth Expired (401)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_AuthExpired401_GracefulFail()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Invalid API Token""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());

        // Assert
        result.IsSuccess.Should().BeFalse("401 should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 3: Rate Limit (429)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_RateLimit429_GracefulFail()
    {
        // Arrange — OpenCart's Polly pipeline handles 429 via retry
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "30")
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Too many requests""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());

        // Assert — OpenCart Polly retries on 429, should eventually fail
        result.IsSuccess.Should().BeFalse("429 rate limit should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 4: Server Error (500)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_ServerError500_GracefulFail()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Internal Server Error""}"));

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
        // Arrange
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
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
        var adapter = new OpenCartAdapter(httpClient, _logger);
        var creds = new Dictionary<string, string>
        {
            ["ApiToken"] = "oc-token",
            ["BaseUrl"] = "http://127.0.0.1:1"
        };

        // Act
        var result = await adapter.TestConnectionAsync(creds);

        // Assert
        result.IsSuccess.Should().BeFalse("network error should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
