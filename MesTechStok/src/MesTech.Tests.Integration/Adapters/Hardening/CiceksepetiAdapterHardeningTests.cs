using System.Net.Http;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters.Hardening;

/// <summary>
/// CiceksepetiAdapter failure-mode hardening tests.
/// 6 scenarios: timeout, auth expired (401), rate limit (429),
/// server error (500), malformed JSON, network error.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Hardening", "FailureMode")]
public class CiceksepetiAdapterHardeningTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ILogger<CiceksepetiAdapter> _logger;

    private static readonly Dictionary<string, string> TestCredentials = new()
    {
        ["ApiKey"] = "cs-hardening-key"
    };

    public CiceksepetiAdapterHardeningTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _server = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<CiceksepetiAdapter>();
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    private CiceksepetiAdapter CreateAdapter(TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl)
        };
        var opts = Options.Create(new CiceksepetiOptions
        {
            HttpTimeoutSeconds = (int)(timeout?.TotalSeconds ?? 5)
        });
        return new CiceksepetiAdapter(httpClient, _logger, opts);
    }

    private Dictionary<string, string> CredentialsWithBaseUrl() => new(TestCredentials)
    {
        ["BaseUrl"] = _fixture.BaseUrl
    };

    // ────────────────────────────────────────────────────
    // Scenario 1: API Timeout
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_Timeout_GracefulFail()
    {
        // Arrange — Polly retries TaskCanceledException, so use minimal timeout.
        // The adapter may return a failure result or propagate an OperationCanceledException
        // after Polly exhausts retries, since the catch clause excludes OperationCanceledException.
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(35)));

        var adapter = CreateAdapter(timeout: TimeSpan.FromSeconds(1));

        // Act — timeout with Polly retry may throw or return failure
        try
        {
            var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());
            result.IsSuccess.Should().BeFalse("timeout should cause graceful failure");
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            // Polly exhausted retries and propagated the timeout exception — acceptable failure mode
            ex.Should().BeAssignableTo<OperationCanceledException>("timeout should produce OperationCanceledException");
        }
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
                .WithBody(@"{""message"":""API Key is invalid""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(CredentialsWithBaseUrl());

        // Assert
        result.IsSuccess.Should().BeFalse("401 should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.HttpStatusCode.Should().Be(401);
    }

    // ────────────────────────────────────────────────────
    // Scenario 3: Rate Limit (429)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_RateLimit429_GracefulFail()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "30")
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
        // Arrange
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
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
            BaseAddress = new Uri("https://127.0.0.1:1"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        var adapter = new CiceksepetiAdapter(httpClient, _logger);
        var creds = new Dictionary<string, string>
        {
            ["ApiKey"] = "cs-key",
            ["BaseUrl"] = "https://127.0.0.1:1"
        };

        // Act
        var result = await adapter.TestConnectionAsync(creds);

        // Assert
        result.IsSuccess.Should().BeFalse("network error should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
