using System.Net;
using FluentAssertions;
using MesTech.Application.Features.Health.Queries.GetMesaStatus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetMesaStatusHandlerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<GetMesaStatusHandler>> _loggerMock = new();
    private readonly GetMesaStatusHandler _sut;

    public GetMesaStatusHandlerTests()
    {
        _configMock.Setup(c => c["Mesa:BridgeUrl"]).Returns("http://localhost:3105");
        _sut = new GetMesaStatusHandler(
            _httpClientFactoryMock.Object,
            _configMock.Object,
            _loggerMock.Object);
    }

    private static HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return new HttpClient(handlerMock.Object);
    }

    [Fact]
    public async Task Handle_HealthyBridge_ReturnsConnected()
    {
        // Arrange
        var json = """{"LastHeartbeat":"2026-03-30T10:00:00Z","Version":"1.2.3","ActiveConsumers":5,"FeatureFlags":{"ai":true}}""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        _httpClientFactoryMock.Setup(f => f.CreateClient("MesaBridge"))
            .Returns(CreateMockHttpClient(response));

        var query = new GetMesaStatusQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsConnected.Should().BeTrue();
        result.Version.Should().Be("1.2.3");
        result.ActiveConsumers.Should().Be(5);
        result.BridgeUrl.Should().Be("http://localhost:3105");
        result.ResponseTimeMs.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
        result.FeatureFlags.Should().ContainKey("ai").WhoseValue.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Non200Response_ReturnsDisconnectedWithStatusCode()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            ReasonPhrase = "Service Unavailable"
        };
        _httpClientFactoryMock.Setup(f => f.CreateClient("MesaBridge"))
            .Returns(CreateMockHttpClient(response));

        var query = new GetMesaStatusQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsConnected.Should().BeFalse();
        result.ErrorMessage.Should().Contain("503");
        result.ResponseTimeMs.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_HttpRequestException_ReturnsDisconnected()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        _httpClientFactoryMock.Setup(f => f.CreateClient("MesaBridge"))
            .Returns(new HttpClient(handlerMock.Object));

        var query = new GetMesaStatusQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsConnected.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection refused");
        result.BridgeUrl.Should().Be("http://localhost:3105");
    }

    [Fact]
    public async Task Handle_TaskCanceledException_ReturnsDisconnected()
    {
        // Arrange — simulates timeout
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        _httpClientFactoryMock.Setup(f => f.CreateClient("MesaBridge"))
            .Returns(new HttpClient(handlerMock.Object));

        var query = new GetMesaStatusQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsConnected.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_NoConfiguredBridgeUrl_UsesDefaultLocalhost()
    {
        // Arrange — config returns null
        _configMock.Setup(c => c["Mesa:BridgeUrl"]).Returns((string?)null);

        var json = """{"Version":"0.1.0","ActiveConsumers":0}""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        _httpClientFactoryMock.Setup(f => f.CreateClient("MesaBridge"))
            .Returns(CreateMockHttpClient(response));

        var query = new GetMesaStatusQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.BridgeUrl.Should().Be("http://localhost:3105");
        result.IsConnected.Should().BeTrue();
    }
}
