using FluentAssertions;
using MesTech.Application.Commands.ApproveReturn;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// ApproveReturnHandler: Z5 iade zinciri — iade onay + stok geri yükleme.
/// Kritik iş kuralları:
///   - Sadece Pending durumdaki iade onaylanabilir
///   - AutoRestoreStock: iade ürünlerin stoğu geri eklenir
///   - StockRestored flag — çift stok yükleme koruması
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "ReturnChain")]
public class ApproveReturnHandlerTests
{
    private readonly Mock<IReturnRequestRepository> _returnRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public ApproveReturnHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _returnRepo.Setup(r => r.UpdateAsync(It.IsAny<ReturnRequest>())).Returns(Task.CompletedTask);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
    }

    private ApproveReturnHandler CreateHandler() =>
        new(_returnRepo.Object, _productRepo.Object, _uow.Object);

    private (ReturnRequest request, Product product) CreateReturnWithProduct(int productStock = 50)
    {
        var product = new Product
        {
            Name = "İade Ürün",
            SKU = "RET-001",
            PurchasePrice = 50m,
            SalePrice = 100m,
            CategoryId = Guid.NewGuid()
        };
        if (productStock > 0)
            product.AdjustStock(productStock, StockMovementType.StockIn);

        var returnReq = new ReturnRequest
        {
            TenantId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerName = "Test Müşteri",
            Platform = PlatformType.Trendyol
        };
        returnReq.AddLine(new ReturnRequestLine
        {
            TenantId = returnReq.TenantId,
            ProductId = product.Id,
            ProductName = product.Name,
            SKU = product.SKU,
            Quantity = 3,
            UnitPrice = 100m,
            RefundAmount = 300m
        });

        return (returnReq, product);
    }

    [Fact]
    public async Task Handle_ValidReturn_ApprovesAndRestoresStock()
    {
        // Arrange
        var (returnReq, product) = CreateReturnWithProduct(productStock: 10);
        _returnRepo.Setup(r => r.GetByIdAsync(returnReq.Id)).ReturnsAsync(returnReq);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new ApproveReturnCommand(returnReq.Id, AutoRestoreStock: true);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StockRestored.Should().BeTrue();
        product.Stock.Should().Be(13); // 10 + 3 (iade)
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnNotFound_ReturnsFailure()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _returnRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((ReturnRequest?)null);

        var cmd = new ApproveReturnCommand(missingId);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task Handle_AutoRestoreStockFalse_DoesNotRestoreStock()
    {
        // Arrange
        var (returnReq, product) = CreateReturnWithProduct(productStock: 10);
        _returnRepo.Setup(r => r.GetByIdAsync(returnReq.Id)).ReturnsAsync(returnReq);

        var cmd = new ApproveReturnCommand(returnReq.Id, AutoRestoreStock: false);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StockRestored.Should().BeFalse();
        product.Stock.Should().Be(10); // stok DEĞİŞMEDİ
    }

    [Fact]
    public async Task Handle_AlreadyApproved_ThrowsInvalidOperation()
    {
        // Arrange — önce onayla, sonra tekrar onayla
        var (returnReq, _) = CreateReturnWithProduct();
        returnReq.Approve(); // ilk onay — şimdi Approved durumda
        _returnRepo.Setup(r => r.GetByIdAsync(returnReq.Id)).ReturnsAsync(returnReq);

        var cmd = new ApproveReturnCommand(returnReq.Id);
        var handler = CreateHandler();

        // Act & Assert — çift onay InvalidOperationException fırlatmalı
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AutoRestoreDisabled_StockUnchanged()
    {
        // Arrange — AutoRestoreStock = false
        var (returnReq, product) = CreateReturnWithProduct(productStock: 10);
        _returnRepo.Setup(r => r.GetByIdAsync(returnReq.Id)).ReturnsAsync(returnReq);

        var cmd = new ApproveReturnCommand(returnReq.Id, AutoRestoreStock: false);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert — stok değişmemeli (restore devre dışı)
        result.IsSuccess.Should().BeTrue();
        result.StockRestored.Should().BeFalse();
        product.Stock.Should().Be(10);
    }
}
