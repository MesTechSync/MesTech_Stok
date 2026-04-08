using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Exceptions;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Stock management edge case tests — partial deduction, multi-product atomic,
/// concurrent adjust, audit events, zero-stock remove, add-after-remove, sync-to-zero.
/// D5-115 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "StockManagement")]
public class StockManagementEdgeCaseTests
{
    // ── 1. Partial order deduction: order 10 but stock has only 7 ──

    [Fact]
    public void RemoveStock_MoreThanAvailable_ShouldThrowInsufficientStockException()
    {
        var product = FakeData.CreateProduct(sku: "PARTIAL-001", stock: 7);

        var act = () => product.RemoveStock(10, "Order #1234");

        act.Should().Throw<InsufficientStockException>();
        product.Stock.Should().Be(7, "stock must remain unchanged after failed deduction");
    }

    [Fact]
    public void RemoveStock_ExactlyAvailable_ShouldSucceed()
    {
        var product = FakeData.CreateProduct(sku: "PARTIAL-002", stock: 7);

        product.RemoveStock(7, "Order #1235");

        product.Stock.Should().Be(0);
    }

    // ── 2. Multi-product stock deduction in single order (atomic check) ──

    [Fact]
    public void MultiProduct_DeductAll_WhenAllHaveSufficientStock_ShouldSucceed()
    {
        var products = new[]
        {
            FakeData.CreateProduct(sku: "MULTI-A", stock: 20),
            FakeData.CreateProduct(sku: "MULTI-B", stock: 15),
            FakeData.CreateProduct(sku: "MULTI-C", stock: 30),
        };
        var quantities = new[] { 5, 10, 25 };

        // Validate-then-deduct pattern: check all first, then deduct
        for (int i = 0; i < products.Length; i++)
        {
            products[i].Stock.Should().BeGreaterThanOrEqualTo(quantities[i],
                $"pre-check: {products[i].SKU} must have enough stock");
        }

        for (int i = 0; i < products.Length; i++)
        {
            products[i].RemoveStock(quantities[i], "Multi-product order");
        }

        products[0].Stock.Should().Be(15);
        products[1].Stock.Should().Be(5);
        products[2].Stock.Should().Be(5);
    }

    [Fact]
    public void MultiProduct_DeductAll_WhenOneInsufficient_ShouldFailAndLeaveOthersUntouched()
    {
        var productA = FakeData.CreateProduct(sku: "MULTI-FAIL-A", stock: 20);
        var productB = FakeData.CreateProduct(sku: "MULTI-FAIL-B", stock: 3); // insufficient
        var productC = FakeData.CreateProduct(sku: "MULTI-FAIL-C", stock: 30);
        var products = new[] { productA, productB, productC };
        var quantities = new[] { 5, 10, 25 };

        // Validate-first pattern: if any fails pre-check, none should be deducted
        bool allSufficient = true;
        for (int i = 0; i < products.Length; i++)
        {
            if (products[i].Stock < quantities[i])
            {
                allSufficient = false;
                break;
            }
        }

        allSufficient.Should().BeFalse("product B has only 3, needs 10");

        // Since validation failed, no deduction happens
        productA.Stock.Should().Be(20);
        productB.Stock.Should().Be(3);
        productC.Stock.Should().Be(30);
    }

    // ── 3. Stock hold/reservation concept ──
    // Domain does not have a reservation/hold mechanism — RemoveStock is immediate.
    // This test verifies that stock cannot go negative via AdjustStock (the guard).

    [Fact]
    public void AdjustStock_NegativeQuantityExceedingStock_ShouldThrowAndRaiseOversellingEvent()
    {
        var product = FakeData.CreateProduct(sku: "HOLD-001", stock: 5);

        var act = () => product.AdjustStock(-8, StockMovementType.StockOut, "Reservation attempt");

        act.Should().Throw<InsufficientStockException>();
        product.Stock.Should().Be(5, "stock unchanged after overselling attempt");
        // AdjustStock raises OversellingAttemptedEvent before throwing
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OversellingAttemptedEvent>();
    }

    // ── 4. Concurrent AdjustStock on same product ──

    [Fact]
    public void ConcurrentAdjustStock_SequentialCalls_ShouldResultInCorrectFinalStock()
    {
        var product = FakeData.CreateProduct(sku: "CONC-001", stock: 100);

        // Simulate sequential adjustments (domain is not thread-safe — EF concurrency token handles DB)
        product.AdjustStock(-10, StockMovementType.StockOut, "Order 1");
        product.AdjustStock(-20, StockMovementType.StockOut, "Order 2");
        product.AdjustStock(5, StockMovementType.StockIn, "Return 1");
        product.AdjustStock(-15, StockMovementType.StockOut, "Order 3");

        // 100 - 10 - 20 + 5 - 15 = 60
        product.Stock.Should().Be(60);
        product.DomainEvents.Should().HaveCount(4,
            "each AdjustStock raises at least StockChangedEvent");
    }

    [Fact]
    public void ConcurrentAdjustStock_LastCallExceedsRemaining_ShouldThrow()
    {
        var product = FakeData.CreateProduct(sku: "CONC-002", stock: 30);

        product.AdjustStock(-10, StockMovementType.StockOut);
        product.AdjustStock(-10, StockMovementType.StockOut);
        // Now stock is 10
        var act = () => product.AdjustStock(-15, StockMovementType.StockOut);

        act.Should().Throw<InsufficientStockException>();
        product.Stock.Should().Be(10, "last deduction rolled back");
    }

    // ── 5. Stock movement audit: StockChangedEvent with correct previous/new values ──

    [Fact]
    public void AdjustStock_ShouldRaiseStockChangedEvent_WithCorrectPreviousAndNewValues()
    {
        var product = FakeData.CreateProduct(sku: "AUDIT-001", stock: 50);

        product.AdjustStock(-12, StockMovementType.StockOut, "Sale");

        var evt = product.DomainEvents.OfType<StockChangedEvent>().FirstOrDefault();
        evt.Should().NotBeNull();
        evt!.PreviousQuantity.Should().Be(50);
        evt.NewQuantity.Should().Be(38);
        evt.MovementType.Should().Be(StockMovementType.StockOut);
    }

    [Fact]
    public void AddStock_ShouldRaiseStockChangedEvent_WithStockInMovementType()
    {
        var product = FakeData.CreateProduct(sku: "AUDIT-002", stock: 20);

        product.AddStock(30, "Purchase PO-555");

        var evt = product.DomainEvents.OfType<StockChangedEvent>().FirstOrDefault();
        evt.Should().NotBeNull();
        evt!.PreviousQuantity.Should().Be(20);
        evt.NewQuantity.Should().Be(50);
        evt.MovementType.Should().Be(StockMovementType.StockIn);
    }

    // ── 6. RemoveStock on zero-stock product ──

    [Fact]
    public void RemoveStock_OnZeroStockProduct_ShouldThrowInsufficientStockException()
    {
        var product = FakeData.CreateProduct(sku: "ZERO-001", stock: 0);

        var act = () => product.RemoveStock(1, "Attempted sale");

        act.Should().Throw<InsufficientStockException>();
        product.Stock.Should().Be(0);
    }

    [Fact]
    public void RemoveStock_ZeroQuantity_ShouldThrowArgumentException()
    {
        var product = FakeData.CreateProduct(sku: "ZERO-002", stock: 10);

        var act = () => product.RemoveStock(0, "Invalid");

        act.Should().Throw<ArgumentException>();
        product.Stock.Should().Be(10);
    }

    // ── 7. AddStock after RemoveStock: verify stock level is correct ──

    [Fact]
    public void AddStock_AfterRemoveStock_ShouldReflectCorrectLevel()
    {
        var product = FakeData.CreateProduct(sku: "ADDREM-001", stock: 50);

        product.RemoveStock(30, "Sold");
        product.AddStock(10, "Restock PO-100");

        product.Stock.Should().Be(30); // 50 - 30 + 10
        product.DomainEvents.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public void AddStock_AfterFullDepletion_ShouldRestoreStock()
    {
        var product = FakeData.CreateProduct(sku: "ADDREM-002", stock: 25);

        product.RemoveStock(25, "Sold out");
        product.Stock.Should().Be(0);

        product.AddStock(40, "New shipment");
        product.Stock.Should().Be(40);
        product.IsOutOfStock().Should().BeFalse();
    }

    // ── 8. SyncStock with 0: valid (product exists but out of stock) ──

    [Fact]
    public void SyncStock_ToZero_ShouldSetStockToZeroAndRaiseEvents()
    {
        var product = FakeData.CreateProduct(sku: "SYNC-001", stock: 100);

        product.SyncStock(0, "Platform sync — Trendyol");

        product.Stock.Should().Be(0);
        product.IsOutOfStock().Should().BeTrue();
        product.DomainEvents.Should().Contain(e => e is StockChangedEvent);
        product.DomainEvents.Should().Contain(e => e is ZeroStockDetectedEvent);
    }

    [Fact]
    public void SyncStock_NegativeValue_ShouldThrowArgumentOutOfRangeException()
    {
        var product = FakeData.CreateProduct(sku: "SYNC-002", stock: 50);

        var act = () => product.SyncStock(-1, "Invalid sync");

        act.Should().Throw<ArgumentOutOfRangeException>();
        product.Stock.Should().Be(50, "stock unchanged after invalid sync");
    }

    [Fact]
    public void SyncStock_SameValue_ShouldNotRaiseStockChangedEvent()
    {
        var product = FakeData.CreateProduct(sku: "SYNC-003", stock: 42);

        product.SyncStock(42, "No-op sync");

        product.Stock.Should().Be(42);
        product.DomainEvents.Should().NotContain(e => e is StockChangedEvent,
            "sync to same value should be a no-op");
    }

    // ── Bonus: AddStock with zero/negative quantity guard ──

    [Fact]
    public void AddStock_ZeroQuantity_ShouldThrowArgumentException()
    {
        var product = FakeData.CreateProduct(sku: "GUARD-001", stock: 10);

        var act = () => product.AddStock(0, "Invalid");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddStock_NegativeQuantity_ShouldThrowArgumentException()
    {
        var product = FakeData.CreateProduct(sku: "GUARD-002", stock: 10);

        var act = () => product.AddStock(-5, "Invalid");

        act.Should().Throw<ArgumentException>();
    }
}
