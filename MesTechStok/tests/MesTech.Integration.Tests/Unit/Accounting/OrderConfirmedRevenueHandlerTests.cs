using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// OrderConfirmedRevenueHandler: Z2 zinciri — sipariş onayı → gelir kaydı.
/// Kritik iş kuralları:
///   - Income entity oluşturulmalı
///   - IncomeType = Satis
///   - OrderId ile linkli
///   - Tutar doğru set edilmeli
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "AccountingChain")]
public class OrderConfirmedRevenueHandlerTests
{
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<OrderConfirmedRevenueHandler>> _logger = new();

    public OrderConfirmedRevenueHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _incomeRepo.Setup(r => r.AddAsync(It.IsAny<Income>())).Returns(Task.CompletedTask);
    }

    private OrderConfirmedRevenueHandler CreateHandler() =>
        new(_incomeRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ValidOrder_CreatesIncomeRecord()
    {
        Income? captured = null;
        _incomeRepo.Setup(r => r.AddAsync(It.IsAny<Income>()))
            .Callback<Income>(i => captured = i)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await handler.HandleAsync(orderId, tenantId, "ORD-001", 1500m, null, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.OrderId.Should().Be(orderId);
        captured.IncomeType.Should().Be(IncomeType.Satis);
        captured.Description.Should().Contain("ORD-001");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroAmount_StillCreatesRecord()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-002", 0m, null, CancellationToken.None);

        _incomeRepo.Verify(r => r.AddAsync(It.IsAny<Income>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithStoreId_LinksToStore()
    {
        Income? captured = null;
        _incomeRepo.Setup(r => r.AddAsync(It.IsAny<Income>()))
            .Callback<Income>(i => captured = i)
            .Returns(Task.CompletedTask);

        var storeId = Guid.NewGuid();
        var handler = CreateHandler();

        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-003", 2500m, storeId, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.StoreId.Should().Be(storeId);
    }
}
