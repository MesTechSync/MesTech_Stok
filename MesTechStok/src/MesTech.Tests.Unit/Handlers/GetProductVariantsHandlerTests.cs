using FluentAssertions;
using MesTech.Application.Features.Product.Queries.GetProductVariants;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetProductVariantsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ILogger<GetProductVariantsHandler>> _loggerMock = new();
    private readonly GetProductVariantsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetProductVariantsHandlerTests()
    {
        _sut = new GetProductVariantsHandler(_productRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsEmptyMatrix()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.Handle(
            new GetProductVariantsQuery(_tenantId, Guid.NewGuid()), CancellationToken.None);

        result.Should().NotBeNull();
        result.Variants.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TenantMismatch_ReturnsEmptyMatrix()
    {
        var product = new Product { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Name = "Test" };
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _sut.Handle(
            new GetProductVariantsQuery(_tenantId, product.Id), CancellationToken.None);

        result.Variants.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProductWithVariants_ReturnsMappedMatrix()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            TenantId = _tenantId,
            Name = "T-Shirt",
            Variants = new List<ProductVariant>
            {
                CreateVariant(productId, "TSH-S-RED", 10, 49.90m),
                CreateVariant(productId, "TSH-M-BLUE", 5, 54.90m)
            }
        };
        _productRepoMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _sut.Handle(
            new GetProductVariantsQuery(_tenantId, productId), CancellationToken.None);

        result.ProductName.Should().Be("T-Shirt");
        result.Variants.Should().HaveCount(2);
        result.TotalStock.Should().Be(15);
        result.Variants.Should().HaveCountGreaterOrEqualTo(2);
    }

    private static ProductVariant CreateVariant(Guid productId, string sku, int stock, decimal price)
    {
        var v = ProductVariant.Create(productId, sku, stock, price);
        return v;
    }
}
