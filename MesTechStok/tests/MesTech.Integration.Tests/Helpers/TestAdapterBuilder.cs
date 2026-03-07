using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using Moq;

namespace MesTech.Integration.Tests.Helpers;

public class TestAdapterBuilder
{
    private readonly Mock<IIntegratorAdapter> _mock = new();
    private string _platformCode = "TestPlatform";
    private bool _supportsStock = true;
    private bool _supportsPrice = true;
    private bool _supportsShipment = false;

    public TestAdapterBuilder WithPlatformCode(string code)
    {
        _platformCode = code;
        return this;
    }

    public TestAdapterBuilder WithStockSupport(bool supports = true)
    {
        _supportsStock = supports;
        return this;
    }

    public TestAdapterBuilder WithPriceSupport(bool supports = true)
    {
        _supportsPrice = supports;
        return this;
    }

    public TestAdapterBuilder WithShipmentSupport(bool supports = true)
    {
        _supportsShipment = supports;
        return this;
    }

    public TestAdapterBuilder WithPullProductsResult(IReadOnlyList<Product> products)
    {
        _mock.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);
        return this;
    }

    public TestAdapterBuilder WithPushStockResult(bool success)
    {
        _mock.Setup(a => a.PushStockUpdateAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success);
        return this;
    }

    public TestAdapterBuilder WithPushPriceResult(bool success)
    {
        _mock.Setup(a => a.PushPriceUpdateAsync(
            It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success);
        return this;
    }

    public TestAdapterBuilder WithConnectionTestResult(ConnectionTestResultDto result)
    {
        _mock.Setup(a => a.TestConnectionAsync(
            It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return this;
    }

    public Mock<IIntegratorAdapter> Build()
    {
        _mock.Setup(a => a.PlatformCode).Returns(_platformCode);
        _mock.Setup(a => a.SupportsStockUpdate).Returns(_supportsStock);
        _mock.Setup(a => a.SupportsPriceUpdate).Returns(_supportsPrice);
        _mock.Setup(a => a.SupportsShipment).Returns(_supportsShipment);
        return _mock;
    }

    public IIntegratorAdapter BuildObject() => Build().Object;
}
