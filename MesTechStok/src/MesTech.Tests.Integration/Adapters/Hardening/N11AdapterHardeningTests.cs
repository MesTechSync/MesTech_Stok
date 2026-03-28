using System.Net.Http;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters.Hardening;

/// <summary>
/// N11Adapter failure-mode hardening tests.
/// N11 uses SOAP/XML via SimpleSoapClient backed by HttpClient.
/// 6 scenarios: timeout, auth error, rate limit, server error, malformed XML, network error.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Hardening", "FailureMode")]
public class N11AdapterHardeningTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ILogger<N11Adapter> _logger;
    private readonly LoggerFactory _loggerFactory;

    public N11AdapterHardeningTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _server = fixture.Server;
        _loggerFactory = new LoggerFactory();
        _logger = _loggerFactory.CreateLogger<N11Adapter>();
    }

    public void Dispose()
    {
        _fixture.Reset();
        _loggerFactory.Dispose();
    }

    private static Mock<IHttpClientFactory> CreateMockFactory()
    {
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        return mock;
    }

    private N11Adapter CreateAdapter(TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        };
        var adapter = new N11Adapter(_logger, CreateMockFactory().Object);
        adapter.Configure("test-app-key", "test-app-secret", _fixture.BaseUrl, httpClient);
        return adapter;
    }

    private Dictionary<string, string> TestCredentials() => new()
    {
        ["N11AppKey"] = "test-app-key",
        ["N11AppSecret"] = "test-app-secret",
        ["N11BaseUrl"] = _fixture.BaseUrl
    };

    // ────────────────────────────────────────────────────
    // Scenario 1: API Timeout
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_Timeout_GracefulFail()
    {
        // Arrange — SOAP endpoint delays beyond HttpClient timeout
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(35)));

        var adapter = new N11Adapter(_logger, CreateMockFactory().Object);
        var creds = TestCredentials();
        creds["N11BaseUrl"] = _fixture.BaseUrl;

        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        adapter.Configure(creds["N11AppKey"], creds["N11AppSecret"], _fixture.BaseUrl, httpClient);

        // Act
        var result = await adapter.TestConnectionAsync(creds);

        // Assert
        result.IsSuccess.Should().BeFalse("timeout should cause graceful failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 2: Auth Error (SOAP Fault with auth message)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_AuthExpired_GracefulFail()
    {
        // Arrange — SOAP Fault simulating auth failure
        var soapFault = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soapenv:Body>
    <soapenv:Fault>
      <faultcode>Client</faultcode>
      <faultstring>Authentication Failed: Invalid API Key or Secret</faultstring>
    </soapenv:Fault>
  </soapenv:Body>
</soapenv:Envelope>";

        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml; charset=utf-8")
                .WithBody(soapFault));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials());

        // Assert
        result.IsSuccess.Should().BeFalse("SOAP auth fault should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 3: Rate Limit (429)
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_RateLimit429_GracefulFail()
    {
        // Arrange — HTTP 429 before SOAP processing
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "60")
                .WithBody("Rate limit exceeded"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials());

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
                .WithBody("Internal Server Error"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials());

        // Assert
        result.IsSuccess.Should().BeFalse("500 should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────
    // Scenario 5: Malformed XML Response
    // ────────────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_MalformedXml_GracefulFail()
    {
        // Arrange — 200 OK but invalid XML
        _server
            .Given(Request.Create().WithPath("/*").UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody("this is not valid xml <<<>>>"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(TestCredentials());

        // Assert
        result.IsSuccess.Should().BeFalse("malformed XML should cause failure");
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
            Timeout = TimeSpan.FromSeconds(5)
        };
        var adapter = CreateAdapter();
        adapter.Configure("test-key", "test-secret", "http://127.0.0.1:1", httpClient);

        var creds = new Dictionary<string, string>
        {
            ["N11AppKey"] = "test-key",
            ["N11AppSecret"] = "test-secret",
            ["N11BaseUrl"] = "http://127.0.0.1:1"
        };

        // Act
        var result = await adapter.TestConnectionAsync(creds);

        // Assert
        result.IsSuccess.Should().BeFalse("network error should cause failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
