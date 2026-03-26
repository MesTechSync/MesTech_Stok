using FluentAssertions;
using MesTech.Application.Queries.GetProductById;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetProductByIdHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly GetProductByIdHandler _sut;

    public GetProductByIdHandlerTests()
    {
        _sut = new GetProductByIdHandler(_productRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsDto()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Ürün",
            SKU = "SKU-001",
            PurchasePrice = 50m,
            SalePrice = 100m,
            Stock = 10,
            MinimumStock = 5,
            IsActive = true
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        var query = new GetProductByIdQuery(productId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Ürün");
        result.SKU.Should().Be("SKU-001");
    }

    [Fact]
    public async Task Handle_NonExistentProduct_ReturnsNull()
    {
        var productId = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        var query = new GetProductByIdQuery(productId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        var act = () => new GetProductByIdHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
