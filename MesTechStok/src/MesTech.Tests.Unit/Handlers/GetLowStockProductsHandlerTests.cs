using FluentAssertions;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetLowStockProductsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly GetLowStockProductsHandler _sut;

    public GetLowStockProductsHandlerTests()
    {
        _sut = new GetLowStockProductsHandler(_productRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsLowStockProducts()
    {
        var p1 = new Product { Id = Guid.NewGuid(), Name = "Düşük Stok A", MinimumStock = 10 };
        p1.SyncStock(2);
        var p2 = new Product { Id = Guid.NewGuid(), Name = "Düşük Stok B", MinimumStock = 5 };
        var products = new List<Product> { p1, p2 };
        _productRepoMock.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products.AsReadOnly());

        var query = new GetLowStockProductsQuery();
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoLowStock_ReturnsEmpty()
    {
        _productRepoMock.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>().AsReadOnly());

        var query = new GetLowStockProductsQuery();
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
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
        var act = () => new GetLowStockProductsHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
