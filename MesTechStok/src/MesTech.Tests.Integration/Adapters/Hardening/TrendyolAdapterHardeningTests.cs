using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters.Hardening;

/// <summary>
/// TrendyolAdapter failure-mode hardening tests.
/// 6 scenarios: timeout, auth expired (401), rate limit (429),
/// server error (500), malformed JSON, network error.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Hardening", "FailureMode")]
public class TrendyolAdapterHardeningTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ILogger<TrendyolAdapter> _logger;

    private const string SupplierId = "99999";

    private static readonly Dictionary<string, string> TestCredentials = new()
    {
        ["ApiKey"] = "hardening-key",
        ["ApiSecret"] = "hardening-secret",
        ["SupplierId"] = SupplierId
    };

    public TrendyolAdapterHardeningTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _server = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<TrendyolAdapter>();
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    private TrendyolAdapter CreateAdapter(TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_fixture.BaseUrl),
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        };
        return new TrendyolAdapter(httpClient, _logger);
    }

    // ────────────────────────────────────────────────────
    // Scenario 1: API Timeout
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_Timeout_GracefulFail()
    {
        // Arrange — WireMock responds with 35s delay; HttpClient timeout is 3s
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(35)));

        var adapter = CreateAdapter(timeout: TimeSpan.FromSeconds(3));

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials);

        // Assert — should not throw, should return error
        result.IsSuccess.Should().BeFalse("timeout should cause graceful failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty("error message should explain the timeout");
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
                .WithBody(@"{""error"":""Unauthorized"",""message"":""Token expired""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials);

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
                .WithHeader("Retry-After", "60")
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Too Many Requests""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials);

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
                .WithBody(@"{""error"":""Internal Server Error""}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials);

        // Assert — Polly retry may fire but eventually should fail
        result.IsSuccess.Should().BeFalse("500 server error should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 5: Malformed JSON Response
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_MalformedJson_GracefulFail()
    {
        // Arrange — 200 OK but invalid JSON body
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("this is not valid json {{{"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials);

        // Assert — JSON parse failure should be caught gracefully
        result.IsSuccess.Should().BeFalse("malformed JSON should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty("error message should explain the parse failure");
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
        var adapter = new TrendyolAdapter(httpClient, _logger);

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials);

        // Assert — HttpRequestException should be caught
        result.IsSuccess.Should().BeFalse("network error should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty("error message should indicate connection failure");
    }
}
