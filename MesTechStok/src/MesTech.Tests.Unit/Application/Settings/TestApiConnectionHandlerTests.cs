using System.Net;
using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.TestApiConnection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Application.Settings;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class TestApiConnectionHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private TestApiConnectionHandler CreateSut(HttpResponseMessage? response = null, Exception? exception = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        var setup = handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        if (exception is not null)
            setup.ThrowsAsync(exception);
        else
            setup.ReturnsAsync(response ?? new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("ApiConnectionTest")).Returns(httpClient);

        return new TestApiConnectionHandler(factoryMock.Object,
            NullLogger<TestApiConnectionHandler>.Instance);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_InvalidUrl_ReturnsFailureWithoutHttpCall()
    {
        var sut = CreateSut();
        var cmd = new TestApiConnectionCommand(_tenantId, "not-a-valid-url");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Gecersiz URL");
    }

    [Fact]
    public async Task Handle_SuccessfulResponse_ReturnsSuccessWithResponseTime()
    {
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK));
        var cmd = new TestApiConnectionCommand(_tenantId, "https://api.example.com/health");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
        result.Message.Should().Contain("basarili");
    }

    [Fact]
    public async Task Handle_ServerError_ReturnsFailureWithStatusCode()
    {
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var cmd = new TestApiConnectionCommand(_tenantId, "https://api.example.com/health");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.Message.Should().Contain("500");
    }

    [Fact]
    public async Task Handle_HttpRequestException_ReturnsConnectionError()
    {
        var sut = CreateSut(exception: new HttpRequestException("Connection refused"));
        var cmd = new TestApiConnectionCommand(_tenantId, "https://unreachable.example.com");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task Handle_Timeout_ReturnsTimeoutError()
    {
        var sut = CreateSut(exception: new TaskCanceledException("Request timeout"));
        var cmd = new TestApiConnectionCommand(_tenantId, "https://slow.example.com");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("zaman asimi");
    }
}
