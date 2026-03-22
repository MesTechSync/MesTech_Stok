using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProducts;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using Moq;

namespace MesTech.Tests.Unit.Application.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Domain", "Products")]
public class GetDropshipProductsHandlerTests
{
    private readonly Mock<IDropshipProductRepository> _productRepo = new();

    private GetDropshipProductsHandler CreateSut() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsMappedDtos()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var product = DropshipProduct.Create(
            tenantId, Guid.NewGuid(), "EXT-001", "Test Product", 100m, 50);

        _productRepo
            .Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipProduct> { product }.AsReadOnly());

        var query = new GetDropshipProductsQuery(tenantId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].ExternalProductId.Should().Be("EXT-001");
        result[0].Title.Should().Be("Test Product");
        result[0].OriginalPrice.Should().Be(100m);
        result[0].StockQuantity.Should().Be(50);
        result[0].IsLinked.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _productRepo
            .Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipProduct>().AsReadOnly());

        var query = new GetDropshipProductsQuery(tenantId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithLinkedFilter_PassesFilterToRepository()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _productRepo
            .Setup(r => r.GetByTenantAsync(tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipProduct>().AsReadOnly());

        var query = new GetDropshipProductsQuery(tenantId, IsLinked: true);
        var sut = CreateSut();

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert
        _productRepo.Verify(
            r => r.GetByTenantAsync(tenantId, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
