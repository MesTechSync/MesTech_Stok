using FluentAssertions;
using MesTech.Application.Commands.AdjustStock;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class AdjustStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo;
    private readonly Mock<IStockMovementRepository> _movementRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly AdjustStockHandler _sut;

    public AdjustStockHandlerTests()
    {
        _productRepo = new Mock<IProductRepository>();
        _movementRepo = new Mock<IStockMovementRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new AdjustStockHandler(_productRepo.Object, _movementRepo.Object, _uow.Object, Mock.Of<IDistributedLockService>(), Mock.Of<ILogger<AdjustStockHandler>>());
    }

    [Fact]
    public async Task Handle_ValidProduct_ReturnsSuccessAndRecordsMovement()
    {
        // Arrange
        var product = new Product { Name = "Test", SKU = "SKU-001", Stock = 50 };
        var productId = product.Id;
        _productRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        var command = new AdjustStockCommand(productId, 10, "Recount", "admin");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Once());
        _productRepo.Verify(r => r.UpdateAsync(product), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        var command = new AdjustStockCommand(productId, 5);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(productId.ToString());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
