using FluentAssertions;
using MesTech.Application.Commands.AddStockLot;
using MesTech.Application.Commands.ConvertQuotationToInvoice;
using MesTech.Application.Queries.GetInventoryValue;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for AddStockLotHandler, GetInventoryValueHandler, and ConvertQuotationToInvoiceHandler.
/// </summary>
[Trait("Category", "Unit")]
public class StockLotHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    // ═══════ AddStockLotHandler ═══════

    [Fact]
    public async Task AddStockLot_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new AddStockLotHandler(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task AddStockLot_ZeroQuantity_ReturnsFailure()
    {
        var sut = new AddStockLotHandler(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object);
        var cmd = new AddStockLotCommand(Guid.NewGuid(), "LOT-001", 0, 10m);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("pozitif");
    }

    [Fact]
    public async Task AddStockLot_EmptyLotNumber_ReturnsFailure()
    {
        var sut = new AddStockLotHandler(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object);
        var cmd = new AddStockLotCommand(Guid.NewGuid(), "", 5, 10m);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Lot numarası");
    }

    [Fact]
    public async Task AddStockLot_ProductNotFound_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        var sut = new AddStockLotHandler(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object);
        var cmd = new AddStockLotCommand(productId, "LOT-001", 5, 10m);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public void AddStockLot_NullProductRepository_ThrowsArgumentNullException()
    {
        var act = () => new AddStockLotHandler(null!, _movementRepo.Object, _unitOfWork.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    // ═══════ GetInventoryValueHandler ═══════

    [Fact]
    public async Task GetInventoryValue_NullRequest_ThrowsArgumentNullException()
    {
        var stockCalc = new StockCalculationService();
        var sut = new GetInventoryValueHandler(_productRepo.Object, stockCalc);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetInventoryValue_EmptyProducts_ReturnsZeros()
    {
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>());
        var stockCalc = new StockCalculationService();
        var sut = new GetInventoryValueHandler(_productRepo.Object, stockCalc);

        var result = await sut.Handle(new GetInventoryValueQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(0);
        result.TotalStock.Should().Be(0);
    }

    [Fact]
    public void GetInventoryValue_NullProductRepository_ThrowsArgumentNullException()
    {
        var stockCalc = new StockCalculationService();
        var act = () => new GetInventoryValueHandler(null!, stockCalc);
        act.Should().Throw<ArgumentNullException>();
    }

    // ═══════ ConvertQuotationToInvoiceHandler ═══════

    private readonly Mock<IQuotationRepository> _quotationRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();

    [Fact]
    public async Task ConvertQuotation_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new ConvertQuotationToInvoiceHandler(_quotationRepo.Object, _invoiceRepo.Object, _unitOfWork.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ConvertQuotation_QuotationNotFound_ReturnsFailure()
    {
        var quotationId = Guid.NewGuid();
        _quotationRepo.Setup(r => r.GetByIdWithLinesAsync(quotationId)).ReturnsAsync((Quotation?)null);

        var sut = new ConvertQuotationToInvoiceHandler(_quotationRepo.Object, _invoiceRepo.Object, _unitOfWork.Object);
        var cmd = new ConvertQuotationToInvoiceCommand(quotationId, "INV-001");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }
}
