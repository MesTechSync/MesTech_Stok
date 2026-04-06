using FluentAssertions;
using MesTech.Application.Queries.GetProductDbStatus;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetProductDbStatusHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ILogger<GetProductDbStatusHandler>> _logger = new();

    private GetProductDbStatusHandler CreateSut() =>
        new(_productRepo.Object, _logger.Object);

    [Fact]
    public async Task Handle_ShouldReturnConnectedTrue_WhenRepoAccessSucceeds()
    {
        var products = new List<Product>
        {
            new() { Name = "P1", SKU = "SKU1", IsActive = true, CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid() },
            new() { Name = "P2", SKU = "SKU2", IsActive = false, CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid() },
            new() { Name = "P3", SKU = "SKU3", IsActive = true, CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid() },
        };
        _productRepo.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();
        var result = await sut.Handle(new GetProductDbStatusQuery(), CancellationToken.None);

        result.IsConnected.Should().BeTrue();
        result.TotalCount.Should().Be(3);
        result.ActiveCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroCounts_WhenNoProducts()
    {
        _productRepo.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>());

        var sut = CreateSut();
        var result = await sut.Handle(new GetProductDbStatusQuery(), CancellationToken.None);

        result.IsConnected.Should().BeTrue();
        result.TotalCount.Should().Be(0);
        result.ActiveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnConnectedFalse_WhenRepoThrows()
    {
        _productRepo.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var sut = CreateSut();
        var result = await sut.Handle(new GetProductDbStatusQuery(), CancellationToken.None);

        result.IsConnected.Should().BeFalse();
        result.TotalCount.Should().Be(0);
        result.ActiveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldCountOnlyActiveProducts()
    {
        var products = new List<Product>
        {
            new() { Name = "Active1", SKU = "A1", IsActive = true, CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid() },
            new() { Name = "Inactive1", SKU = "I1", IsActive = false, CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid() },
            new() { Name = "Inactive2", SKU = "I2", IsActive = false, CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid() },
            new() { Name = "Active2", SKU = "A2", IsActive = true, CategoryId = Guid.NewGuid(), TenantId = Guid.NewGuid() },
        };
        _productRepo.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(4);
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();
        var result = await sut.Handle(new GetProductDbStatusQuery(), CancellationToken.None);

        result.ActiveCount.Should().Be(2);
        result.TotalCount.Should().Be(4);
    }
}
