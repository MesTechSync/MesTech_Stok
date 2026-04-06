using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.TestErpConnection;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Application.Settings;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class TestErpConnectionHandlerTests
{
    private readonly Mock<IErpAdapterFactory> _factory = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private TestErpConnectionHandler CreateSut() => new(
        _factory.Object, NullLogger<TestErpConnectionHandler>.Instance);

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var act = () => CreateSut().Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ProviderNone_ReturnsFailure()
    {
        var cmd = new TestErpConnectionCommand(_tenantId, ErpProvider.None);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("secilmedi");
    }

    [Fact]
    public async Task Handle_PingSuccess_ReturnsSuccessWithResponseTime()
    {
        var adapter = new Mock<IErpAdapter>();
        adapter.Setup(a => a.PingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _factory.Setup(f => f.GetAdapter(ErpProvider.Parasut)).Returns(adapter.Object);

        var cmd = new TestErpConnectionCommand(_tenantId, ErpProvider.Parasut);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
        result.Message.Should().Contain("basarili");
    }

    [Fact]
    public async Task Handle_PingFails_ReturnsFailure()
    {
        var adapter = new Mock<IErpAdapter>();
        adapter.Setup(a => a.PingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _factory.Setup(f => f.GetAdapter(ErpProvider.Parasut)).Returns(adapter.Object);

        var cmd = new TestErpConnectionCommand(_tenantId, ErpProvider.Parasut);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("basarisiz");
    }

    [Fact]
    public async Task Handle_UnsupportedProvider_ReturnsFailure()
    {
        _factory.Setup(f => f.GetAdapter(It.IsAny<ErpProvider>()))
            .Throws(new ArgumentException("Unsupported"));

        var cmd = new TestErpConnectionCommand(_tenantId, ErpProvider.Parasut);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Desteklenmeyen");
    }

    [Fact]
    public async Task Handle_AdapterThrows_ReturnsFailure()
    {
        var adapter = new Mock<IErpAdapter>();
        adapter.Setup(a => a.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        _factory.Setup(f => f.GetAdapter(ErpProvider.Parasut)).Returns(adapter.Object);

        var cmd = new TestErpConnectionCommand(_tenantId, ErpProvider.Parasut);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }
}
