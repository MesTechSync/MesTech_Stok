using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

/// <summary>
/// DEV5: OrderConfirmedRevenueHandler testi — Zincir 2 (sipariş → gelir kaydı).
/// P0 event handler — iş-kritik, çift gelir kaydı önleme (idempotency) dahil.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Chain", "Z2")]
public class OrderConfirmedRevenueHandlerEdgeCaseTests
{
    private readonly Mock<IIncomeRepository> _incomeRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<OrderConfirmedRevenueHandler>> _loggerMock = new();

    private OrderConfirmedRevenueHandler CreateSut() =>
        new(_incomeRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleAsync_HappyPath_ShouldCreateIncomeAndSave()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        _incomeRepoMock
            .Setup(r => r.ExistsByOrderIdAsync(tenantId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Income? capturedIncome = null;
        _incomeRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()))
            .Callback<Income, CancellationToken>((i, _) => capturedIncome = i);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(orderId, tenantId, "ORD-001", 1500.00m, storeId, CancellationToken.None);

        // Assert
        capturedIncome.Should().NotBeNull();
        capturedIncome!.TenantId.Should().Be(tenantId);
        capturedIncome.StoreId.Should().Be(storeId);
        capturedIncome.OrderId.Should().Be(orderId);
        capturedIncome.Amount.Should().Be(1500.00m);
        capturedIncome.IncomeType.Should().Be(IncomeType.Satis);
        capturedIncome.Description.Should().Contain("ORD-001");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroAmount_ShouldSkipWithoutSaving()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-ZERO", 0m, null, CancellationToken.None);

        _incomeRepoMock.Verify(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NegativeAmount_ShouldSkipWithoutSaving()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-NEG", -50m, null, CancellationToken.None);

        _incomeRepoMock.Verify(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DuplicateOrder_ShouldSkipIdempotently()
    {
        // Arrange — income already exists for this order
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _incomeRepoMock
            .Setup(r => r.ExistsByOrderIdAsync(tenantId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(orderId, tenantId, "ORD-DUP", 2000m, Guid.NewGuid(), CancellationToken.None);

        // Assert — no duplicate income created
        _incomeRepoMock.Verify(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NullStoreId_ShouldCreateIncomeWithNullStore()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _incomeRepoMock
            .Setup(r => r.ExistsByOrderIdAsync(tenantId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Income? capturedIncome = null;
        _incomeRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()))
            .Callback<Income, CancellationToken>((i, _) => capturedIncome = i);

        var sut = CreateSut();
        await sut.HandleAsync(orderId, tenantId, "ORD-NOSTORE", 500m, null, CancellationToken.None);

        capturedIncome.Should().NotBeNull();
        capturedIncome!.StoreId.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_SmallAmount_ShouldStillCreateIncome()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _incomeRepoMock
            .Setup(r => r.ExistsByOrderIdAsync(tenantId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        await sut.HandleAsync(orderId, tenantId, "ORD-SMALL", 0.01m, null, CancellationToken.None);

        _incomeRepoMock.Verify(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_IncomeSource_ShouldBeDirectSale()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _incomeRepoMock
            .Setup(r => r.ExistsByOrderIdAsync(tenantId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Income? capturedIncome = null;
        _incomeRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()))
            .Callback<Income, CancellationToken>((i, _) => capturedIncome = i);

        var sut = CreateSut();
        await sut.HandleAsync(orderId, tenantId, "ORD-SRC", 999m, null, CancellationToken.None);

        capturedIncome.Should().NotBeNull();
        capturedIncome!.Source.Should().Be(IncomeSource.DirectSale);
    }
}
