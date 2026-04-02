using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

// ════════════════════════════════════════════════════════
// DEV5: Z5 İade Zinciri — Atomik 3'lü iş mantığı testleri
// Z5a: ReturnApprovedStockRestoration — stok geri yükleme
// Z5b: ReturnJournalReversal — ters muhasebe kaydı
// Z5c: ReturnApprovedRefund — ödeme iadesi
// ════════════════════════════════════════════════════════

#region Z5a: ReturnApprovedStockRestoration

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class ReturnStockRestorationBusinessTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ReturnApprovedStockRestorationHandler>> _logger = new();

    private ReturnApprovedStockRestorationHandler CreateSut() =>
        new(_productRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidReturn_ShouldRestoreStockAndSave()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            SKU = "TST-001", Name = "Test", TenantId = Guid.NewGuid(),
            CreatedBy = "test", UpdatedBy = "test"
        };
        product.AdjustStock(10, global::MesTech.Domain.Enums.StockMovementType.Purchase);
        typeof(global::MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(product, productId);

        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var lines = new List<ReturnLineInfoEvent>
        {
            new(productId, "TST-001", 3, 99.90m)
        };

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        product.Stock.Should().Be(13); // 10 + 3 geri
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldSkipAndContinue()
    {
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var lines = new List<ReturnLineInfoEvent>
        {
            new(Guid.NewGuid(), "MISSING-SKU", 5, 49.90m)
        };

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        // Ürün bulunamadı ama crash yok — SaveChanges çağrılır (boş değişiklik)
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyLines_ShouldStillCallSave()
    {
        var sut = CreateSut();
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), new List<ReturnLineInfoEvent>(), CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Z5b: ReturnJournalReversal

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class ReturnJournalReversalBusinessTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<ILogger<ReturnJournalReversalHandler>> _logger = new();

    private ReturnJournalReversalHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _logger.Object);

    [Fact]
    public async Task Handle_ZeroAmount_ShouldSkipGLEntry()
    {
        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateReference_ShouldSkipIdempotent()
    {
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidReturn_ShouldCreateReversalJournalEntry()
    {
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 750m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Z5c: ReturnApprovedRefund

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class ReturnApprovedRefundBusinessTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ReturnApprovedRefundHandler>> _logger = new();

    private ReturnApprovedRefundHandler CreateSut() =>
        new(_orderRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_ZeroRefundAmount_ShouldSkip()
    {
        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, CancellationToken.None);

        _orderRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldLogErrorAndReturn()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 250m, CancellationToken.None);

        _orderRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        // Order null → refund işlenmez, save çağrılmaz
    }

    [Fact]
    public async Task Handle_NegativeRefund_ShouldSkip()
    {
        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -50m, CancellationToken.None);

        _orderRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion
