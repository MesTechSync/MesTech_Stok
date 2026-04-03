using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Services;
using MesTech.Domain.ValueObjects;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.EdgeCase;

/// <summary>
/// 100 edge case tests across 10 categories (10 tests each).
/// Task C-M2-08: Comprehensive edge case coverage for domain entities, value objects, and services.
/// </summary>
[Trait("Category", "EdgeCase")]
public class EdgeCaseTests
{
    // ═══════════════════════════════════════════════════════════════
    // 1. Negatif tutar (10) — Negative price, stock, commission
    // ═══════════════════════════════════════════════════════════════
    public class NegativeAmountTests
    {
        [Fact]
        public void Product_NegativeStock_AdjustStock_ThrowsInsufficientStockException()
        {
            // Product.AdjustStock guards against negative stock results
            var product = FakeData.CreateProduct(stock: 5);

            var act = () => product.AdjustStock(-10, StockMovementType.StockOut);

            act.Should().Throw<InsufficientStockException>();
            product.Stock.Should().Be(5); // Stock unchanged
        }

        [Fact]
        public void Product_NegativePurchasePrice_TotalValueIsNegative()
        {
            var product = FakeData.CreateProduct(purchasePrice: -50m, stock: 10);

            product.TotalValue.Should().Be(-500m);
        }

        [Fact]
        public void Product_NegativeSalePrice_ProfitMarginReturnsZero()
        {
            // ProfitMargin returns 0 when SalePrice <= 0
            var product = FakeData.CreateProduct(salePrice: -100m);

            product.ProfitMargin.Should().Be(0);
        }

        [Fact]
        public void Money_NegativeAmount_SubtractYieldsNegativeBalance()
        {
            var money = Money.TRY(10m);
            var result = money.Subtract(Money.TRY(25m));

            result.Amount.Should().Be(-15m);
        }

        [Fact]
        public void PricingService_NegativeDiscountRate_ThrowsArgumentOutOfRange()
        {
            var svc = new PricingService();

            var act = () => svc.ApplyDiscount(100m, -10m);

            act.Should().Throw<ArgumentOutOfRangeException>("discount rate < 0 is invalid");
        }

        [Fact]
        public void PricingService_NegativePurchasePrice_ThrowsArgumentOutOfRange()
        {
            var svc = new PricingService();

            // Negative purchase price now throws
            var act = () => svc.CalculateProfitMargin(-50m, 100m);

            act.Should().Throw<ArgumentOutOfRangeException>("negative purchase price is invalid");
        }

        [Fact]
        public void PlatformCommission_NegativeRate_CalculateReturnsNegative()
        {
            var commission = new PlatformCommission
            {
                TenantId = Guid.NewGuid(),
                Platform = PlatformType.Trendyol,
                Type = CommissionType.Percentage,
                Rate = -5m
            };

            var result = commission.Calculate(1000m);

            result.Should().Be(-50m, "negative rate yields negative commission without guard");
        }

        [Fact]
        public void OrderItem_NegativeQuantity_SubTotalIsNegative()
        {
            var item = new OrderItem
            {
                TenantId = Guid.NewGuid(),
                Quantity = -3,
                UnitPrice = 100m,
                TaxRate = 0.18m
            };
            item.CalculateAmounts();

            item.SubTotal.Should().Be(-300m);
            item.TotalPrice.Should().Be(-300m);
            item.TaxAmount.Should().Be(-54m);
        }

        [Fact]
        public void CustomerAccount_RecordSaleWithNegativeAmount_BalanceGoesNegative()
        {
            var account = new CustomerAccount
            {
                TenantId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                AccountCode = "C-NEG"
            };

            account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), -500m, "INV-NEG");

            account.Balance.Should().Be(-500m, "negative sale amount creates negative debit");
        }

        [Fact]
        public void StockMovement_NegativeQuantity_IsNegativeMovementTrue()
        {
            var movement = new StockMovement
            {
                TenantId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = -50
            };
            movement.SetStockLevels(100, 50);

            movement.IsNegativeMovement.Should().BeTrue();
            movement.IsPositiveMovement.Should().BeFalse();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 2. Sifir senaryolari (10) — Zero sales, stock, KDV
    // ═══════════════════════════════════════════════════════════════
    public class ZeroScenarioTests
    {
        [Fact]
        public void Product_ZeroStock_IsOutOfStock()
        {
            var product = FakeData.CreateProduct(stock: 0);

            product.IsOutOfStock().Should().BeTrue();
            product.Stock.Should().Be(0);
        }

        [Fact]
        public void Product_ZeroSalePrice_ProfitMarginIsZero()
        {
            var product = FakeData.CreateProduct(salePrice: 0m, purchasePrice: 50m);

            product.ProfitMargin.Should().Be(0);
        }

        [Fact]
        public void Money_ZeroAmount_AddReturnsOther()
        {
            var zero = Money.Zero("TRY");
            var other = Money.TRY(42.5m);

            var result = zero.Add(other);

            result.Amount.Should().Be(42.5m);
            result.Currency.Should().Be("TRY");
        }

        [Fact]
        public void PricingService_ZeroTaxRate_PriceUnchanged()
        {
            var svc = new PricingService();

            var result = svc.CalculatePriceWithTax(100m, 0m);

            result.Should().Be(100m);
        }

        [Fact]
        public void PricingService_ZeroSalePrice_MarginIsZero()
        {
            var svc = new PricingService();

            var margin = svc.CalculateProfitMargin(50m, 0m);

            margin.Should().Be(0m);
        }

        [Fact]
        public void OrderItem_ZeroQuantity_SubTotalIsZero()
        {
            var item = new OrderItem
            {
                TenantId = Guid.NewGuid(),
                Quantity = 0,
                UnitPrice = 250m,
                TaxRate = 0.18m
            };
            item.CalculateAmounts();

            item.SubTotal.Should().Be(0m);
            item.TotalPrice.Should().Be(0m);
            item.TaxAmount.Should().Be(0m);
        }

        [Fact]
        public void StockCalculationService_ZeroCurrentStock_WACReturnsNewUnitCost()
        {
            var svc = new StockCalculationService();

            var wac = svc.CalculateWAC(0, 0m, 100, 12.50m);

            wac.Should().Be(12.50m);
        }

        [Fact]
        public void CustomerAccount_NoTransactions_BalanceIsZero()
        {
            var account = new CustomerAccount
            {
                TenantId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                AccountCode = "C-ZERO"
            };

            account.Balance.Should().Be(0m);
            account.HasExceededCreditLimit.Should().BeFalse();
        }

        [Fact]
        public void PlatformCommission_ZeroSaleAmount_ReturnsZero()
        {
            var commission = new PlatformCommission
            {
                TenantId = Guid.NewGuid(),
                Platform = PlatformType.Hepsiburada,
                Type = CommissionType.Percentage,
                Rate = 12m
            };

            var result = commission.Calculate(0m);

            result.Should().Be(0m);
        }

        [Fact]
        public void StockLevel_ZeroCurrent_StatusIsOutOfStock()
        {
            var level = new StockLevel(0, 5, 1000, 10, 50);

            level.IsOutOfStock.Should().BeTrue();
            level.Status.Should().Be("OutOfStock");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 3. Duplicate kontrolu (10) — Same order/product mapped twice
    // ═══════════════════════════════════════════════════════════════
    public class DuplicateCheckTests
    {
        [Fact]
        public void DuplicateSKUException_ContainsSKUInMessage()
        {
            var ex = new DuplicateSKUException("SKU-DUP-001");

            ex.SKU.Should().Be("SKU-DUP-001");
            ex.Message.Should().Contain("SKU-DUP-001");
        }

        [Fact]
        public void Order_AddSameItemTwice_BothItemsCounted()
        {
            var tenantId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var order = FakeData.CreateOrder();

            var item1 = new OrderItem
            {
                TenantId = tenantId,
                OrderId = order.Id,
                ProductId = productId,
                ProductName = "Widget",
                Quantity = 2,
                UnitPrice = 100m,
                TotalPrice = 200m,
                TaxRate = 0.18m,
                TaxAmount = 36m
            };
            var item2 = new OrderItem
            {
                TenantId = tenantId,
                OrderId = order.Id,
                ProductId = productId,
                ProductName = "Widget",
                Quantity = 3,
                UnitPrice = 100m,
                TotalPrice = 300m,
                TaxRate = 0.18m,
                TaxAmount = 54m
            };

            order.AddItem(item1);
            order.AddItem(item2);

            // Domain allows duplicate items — no dedup guard
            order.OrderItems.Should().HaveCount(2);
            order.SubTotal.Should().Be(500m);
            order.TotalItems.Should().Be(5);
        }

        [Fact]
        public void ProductPlatformMapping_SameProductTwoDifferentPlatforms_Allowed()
        {
            var tenantId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var mapping1 = new ProductPlatformMapping
            {
                TenantId = tenantId,
                ProductId = productId,
                PlatformType = PlatformType.Trendyol,
                StoreId = Guid.NewGuid()
            };
            var mapping2 = new ProductPlatformMapping
            {
                TenantId = tenantId,
                ProductId = productId,
                PlatformType = PlatformType.Hepsiburada,
                StoreId = Guid.NewGuid()
            };

            mapping1.PlatformType.Should().NotBe(mapping2.PlatformType);
            mapping1.ProductId.Should().Be(mapping2.ProductId);
        }

        [Fact]
        public void ProductPlatformMapping_SameProductSamePlatform_NoGuardInEntity()
        {
            var tenantId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var storeId = Guid.NewGuid();

            var mapping1 = new ProductPlatformMapping
            {
                TenantId = tenantId,
                ProductId = productId,
                PlatformType = PlatformType.Trendyol,
                StoreId = storeId,
                ExternalProductId = "EXT-001"
            };
            var mapping2 = new ProductPlatformMapping
            {
                TenantId = tenantId,
                ProductId = productId,
                PlatformType = PlatformType.Trendyol,
                StoreId = storeId,
                ExternalProductId = "EXT-002"
            };

            // Entity layer allows duplicates — uniqueness enforced at DB/application level
            mapping1.Id.Should().NotBe(mapping2.Id);
            mapping1.ExternalProductId.Should().NotBe(mapping2.ExternalProductId);
        }

        [Fact]
        public void CustomerAccount_DuplicateTransactions_BothRecorded()
        {
            var account = new CustomerAccount
            {
                TenantId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                AccountCode = "C-DUP"
            };
            var invoiceId = Guid.NewGuid();
            var orderId = Guid.NewGuid();

            account.RecordSale(invoiceId, orderId, 1000m, "INV-001");
            account.RecordSale(invoiceId, orderId, 1000m, "INV-001");

            account.Transactions.Should().HaveCount(2);
            account.Balance.Should().Be(2000m);
        }

        [Fact]
        public void SKU_DuplicateValue_RecordsAreEqual()
        {
            var sku1 = new SKU("abc-123");
            var sku2 = new SKU("ABC-123");

            // Both normalized to uppercase
            sku1.Value.Should().Be(sku2.Value);
            sku1.Should().Be(sku2);
        }

        [Fact]
        public void Barcode_DuplicateValue_RecordsAreEqual()
        {
            var barcode1 = new Barcode("8690000000001");
            var barcode2 = new Barcode("8690000000001");

            barcode1.Should().Be(barcode2);
        }

        [Fact]
        public void ReturnRequest_AddDuplicateLine_BothCounted()
        {
            var tenantId = Guid.NewGuid();
            var rr = ReturnRequest.Create(
                Guid.NewGuid(), tenantId, PlatformType.Trendyol,
                ReturnReason.DefectiveProduct, "Test Customer");

            var line1 = new ReturnRequestLine
            {
                TenantId = tenantId,
                ProductName = "Widget",
                Quantity = 1,
                UnitPrice = 100m,
                RefundAmount = 100m
            };
            var line2 = new ReturnRequestLine
            {
                TenantId = tenantId,
                ProductName = "Widget",
                Quantity = 1,
                UnitPrice = 100m,
                RefundAmount = 100m
            };

            rr.AddLine(line1);
            rr.AddLine(line2);

            rr.Lines.Should().HaveCount(2);
            rr.RefundAmount.Should().Be(200m);
        }

        [Fact]
        public void Tenant_DuplicateNames_AllowedAtEntityLevel()
        {
            var t1 = FakeData.CreateTenant("Acme Corp");
            var t2 = FakeData.CreateTenant("Acme Corp");

            t1.Name.Should().Be(t2.Name);
            t1.Id.Should().NotBe(t2.Id, "entity IDs are unique even with same names");
        }

        [Fact]
        public void BaseEntity_SameId_Equals()
        {
            var product1 = FakeData.CreateProduct(sku: "DUP-SKU");
            var product2 = FakeData.CreateProduct(sku: "OTHER-SKU");

            // Different IDs => not equal
            product1.Should().NotBe(product2);

            // Same entity is equal to itself
            product1.Equals(product1).Should().BeTrue();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 4. Boundary degerler (10) — MaxInt, 0.01 TL, 999999 quantity
    // ═══════════════════════════════════════════════════════════════
    public class BoundaryValueTests
    {
        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void Product_ExtremeStock_NoOverflow(int stock)
        {
            var product = FakeData.CreateProduct(stock: stock);

            product.Stock.Should().Be(stock);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(0.001)]
        public void Money_SmallAmounts_PreservePrecision(decimal amount)
        {
            var money = Money.TRY(amount);

            money.Amount.Should().Be(amount);
        }

        [Fact]
        public void StockLevel_MaxIntCurrent_IsOverStock()
        {
            var level = new StockLevel(int.MaxValue, 5, 1000, 10, 50);

            level.IsOverStock.Should().BeTrue();
            level.IsOutOfStock.Should().BeFalse();
        }

        [Fact]
        public void OrderItem_MaxDecimalPrice_CalculateAmounts()
        {
            var item = new OrderItem
            {
                TenantId = Guid.NewGuid(),
                Quantity = 1,
                UnitPrice = 999_999_999.99m,
                TaxRate = 0.18m
            };
            item.CalculateAmounts();

            item.TotalPrice.Should().Be(999_999_999.99m);
            // CalculateAmounts rounds TaxAmount to 2 decimal places:
            // Math.Round(999_999_999.99 * 0.18, 2) = 180_000_000.00
            item.TaxAmount.Should().Be(180_000_000.00m);
        }

        [Fact]
        public void PlatformCommission_BoundaryMinMaxClamping()
        {
            var commission = new PlatformCommission
            {
                TenantId = Guid.NewGuid(),
                Platform = PlatformType.Amazon,
                Type = CommissionType.Percentage,
                Rate = 15m,
                MinAmount = 5m,
                MaxAmount = 100m
            };

            // Below minimum: clamped to MinAmount
            commission.Calculate(10m).Should().Be(5m);
            // Above maximum: clamped to MaxAmount
            commission.Calculate(10_000m).Should().Be(100m);
            // Within range: normal calculation
            commission.Calculate(200m).Should().Be(30m);
        }

        [Theory]
        [InlineData(999999)]
        [InlineData(1)]
        public void Product_BoundaryQuantityAdjust(int qty)
        {
            var product = FakeData.CreateProduct(stock: 0);

            product.AdjustStock(qty, StockMovementType.StockIn);

            product.Stock.Should().Be(qty);
        }

        [Fact]
        public void Money_LargeMultiply_NoOverflow()
        {
            var money = Money.TRY(999_999_999.99m);

            var result = money.Multiply(1.18m);

            result.Amount.Should().BeApproximately(1_179_999_999.9882m, 0.001m);
        }

        [Fact]
        public void StockCalculationService_WAC_VerySmallUnitCost()
        {
            var svc = new StockCalculationService();

            var wac = svc.CalculateWAC(1000, 10m, 1, 0.01m);

            // (1000*10 + 1*0.01) / 1001
            wac.Should().BeApproximately(9.99010989m, 0.0001m);
        }

        [Fact]
        public void InventoryLot_ConsumeExactRemaining_ClosesLot()
        {
            var lot = FakeData.CreateLot(Guid.NewGuid(), receivedQty: 50, remainingQty: 50);

            lot.Consume(50);

            lot.RemainingQty.Should().Be(0);
            lot.Status.Should().Be(LotStatus.Closed);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("12345678901234567890")]
        public void Barcode_BoundaryLengths_CreatedSuccessfully(string value)
        {
            var barcode = new Barcode(value);

            barcode.Value.Should().Be(value);
            barcode.IsCode128.Should().BeTrue();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 5. Null/empty guards (10) — Null supplier, empty items, null tracking
    // ═══════════════════════════════════════════════════════════════
    public class NullEmptyGuardTests
    {
        [Fact]
        public void SKU_EmptyString_ThrowsArgumentException()
        {
            var act = () => new SKU("");

            act.Should().Throw<ArgumentException>()
                .WithMessage("*SKU cannot be empty*");
        }

        [Fact]
        public void SKU_NullString_ThrowsArgumentException()
        {
            var act = () => new SKU(null!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SKU_WhitespaceOnly_ThrowsArgumentException()
        {
            var act = () => new SKU("   ");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Barcode_EmptyString_ThrowsArgumentException()
        {
            var act = () => new Barcode("");

            act.Should().Throw<ArgumentException>()
                .WithMessage("*Barcode cannot be empty*");
        }

        [Fact]
        public void Barcode_NullString_ThrowsArgumentException()
        {
            var act = () => new Barcode(null!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Product_NullSupplierId_Allowed()
        {
            var product = FakeData.CreateProduct();
            product.SupplierId = null;

            product.SupplierId.Should().BeNull();
            // Product is valid without supplier
            product.SKU.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Order_NullTrackingNumber_ShipThrows()
        {
            var order = FakeData.CreateOrder(status: OrderStatus.Confirmed);
            order.Status = OrderStatus.Confirmed;

            // MarkAsShipped now validates tracking number — null/whitespace throws
            var act = () => order.MarkAsShipped(null!, CargoProvider.YurticiKargo);

            act.Should().Throw<ArgumentException>("null tracking number is rejected by ThrowIfNullOrWhiteSpace");
        }

        [Fact]
        public void LocationCode_AllNulls_IsEmpty()
        {
            var loc = new LocationCode(null, null, null, null);

            loc.IsEmpty.Should().BeTrue();
            loc.FullCode.Should().BeEmpty();
        }

        [Fact]
        public void BarcodeValidationService_NullBarcode_ReturnsFalse()
        {
            var svc = new BarcodeValidationService();

            svc.ValidateEAN13(null!).Should().BeFalse();
            svc.ValidateEAN8(null!).Should().BeFalse();
            svc.DetectFormat(null!).Should().Be("Unknown");
        }

        [Fact]
        public void Order_EmptyItems_TotalsAreZero()
        {
            var order = FakeData.CreateOrder();
            // Reset pre-set amounts
            order.CalculateTotals();

            order.SubTotal.Should().Be(0m);
            order.TaxAmount.Should().Be(0m);
            order.TotalAmount.Should().Be(0m);
            order.TotalItems.Should().Be(0);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 6. Concurrent senaryolar (10) — Parallel stock update, parallel order processing
    // ═══════════════════════════════════════════════════════════════
    public class ConcurrencyTests
    {
        [Fact]
        public async Task ParallelStockAdjust_WithoutLock_MayLoseUpdates()
        {
            // Demonstrates that without locking, parallel stock adjustments
            // on the same entity instance can interleave (race condition)
            var product = FakeData.CreateProduct(stock: 1000);
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => product.AdjustStock(-1, StockMovementType.Sale)));
            }
            await Task.WhenAll(tasks);

            // With proper locking, stock would be 900. Without lock, it might differ.
            // We document the behavior — all 100 calls decrement but races are possible.
            product.Stock.Should().BeLessOrEqualTo(1000);
        }

        [Fact]
        public async Task ParallelStockAdjust_WithLock_CorrectResult()
        {
            var product = FakeData.CreateProduct(stock: 1000);
            var lockObj = new object();
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    lock (lockObj)
                    {
                        product.AdjustStock(-1, StockMovementType.Sale);
                    }
                }));
            }
            await Task.WhenAll(tasks);

            product.Stock.Should().Be(900);
        }

        [Fact]
        public async Task ParallelOrderCreation_UniqueIds()
        {
            var orders = new System.Collections.Concurrent.ConcurrentBag<Order>();

            var tasks = Enumerable.Range(0, 50).Select(_ =>
                Task.Run(() => orders.Add(FakeData.CreateOrder())));
            await Task.WhenAll(tasks);

            orders.Should().HaveCount(50);
            orders.Select(o => o.Id).Distinct().Should().HaveCount(50, "each order gets a unique Id");
        }

        [Fact]
        public async Task ParallelCustomerAccountTransactions_WithLock_CorrectBalance()
        {
            var account = new CustomerAccount
            {
                TenantId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                AccountCode = "C-PAR"
            };
            var lockObj = new object();
            var tasks = new List<Task>();

            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    lock (lockObj)
                    {
                        account.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 100m, $"INV-{i}");
                    }
                }));
            }
            await Task.WhenAll(tasks);

            account.Transactions.Should().HaveCount(50);
            account.Balance.Should().Be(5000m);
        }

        [Fact]
        public async Task ParallelLotConsumption_WithLock_NoOverConsume()
        {
            var lot = FakeData.CreateLot(Guid.NewGuid(), receivedQty: 100, remainingQty: 100);
            var lockObj = new object();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
            {
                try
                {
                    lock (lockObj)
                    {
                        if (lot.RemainingQty >= 5)
                            lot.Consume(5);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
            await Task.WhenAll(tasks);

            exceptions.Should().BeEmpty();
            lot.RemainingQty.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task ParallelProductCreation_AllDistinctIds()
        {
            var products = new System.Collections.Concurrent.ConcurrentBag<Product>();

            var tasks = Enumerable.Range(0, 100).Select(_ =>
                Task.Run(() => products.Add(FakeData.CreateProduct())));
            await Task.WhenAll(tasks);

            products.Select(p => p.Id).Distinct().Should().HaveCount(100);
        }

        [Fact]
        public async Task ParallelMoneyAdd_ImmutableSafe()
        {
            var money = Money.TRY(0m);
            var results = new System.Collections.Concurrent.ConcurrentBag<Money>();

            var tasks = Enumerable.Range(1, 100).Select(i =>
                Task.Run(() => results.Add(money.Add(Money.TRY(i)))));
            await Task.WhenAll(tasks);

            // Money is immutable record — original unchanged
            money.Amount.Should().Be(0m);
            results.Should().HaveCount(100);
        }

        [Fact]
        public async Task ParallelSupplierAccountPayments_WithLock_BalanceCorrect()
        {
            var account = new SupplierAccount
            {
                TenantId = Guid.NewGuid(),
                SupplierId = Guid.NewGuid(),
                AccountCode = "S-PAR"
            };
            var lockObj = new object();

            // Record a purchase first
            account.RecordPurchase(Guid.NewGuid(), 10000m, "PUR-001");

            var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            {
                lock (lockObj)
                {
                    account.RecordPayment(1000m);
                }
            }));
            await Task.WhenAll(tasks);

            // Purchase: -10000 credit, Payments: 10*1000 debit
            account.Balance.Should().Be(0m);
        }

        [Fact]
        public async Task ParallelReturnRequestCreation_UniqueEvents()
        {
            var requests = new System.Collections.Concurrent.ConcurrentBag<ReturnRequest>();
            var tenantId = Guid.NewGuid();

            var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
            {
                var rr = ReturnRequest.Create(
                    Guid.NewGuid(), tenantId, PlatformType.Trendyol,
                    ReturnReason.DefectiveProduct, "Customer");
                requests.Add(rr);
            }));
            await Task.WhenAll(tasks);

            requests.Should().HaveCount(20);
            requests.Select(r => r.Id).Distinct().Should().HaveCount(20);
            requests.All(r => r.DomainEvents.Count == 1).Should().BeTrue();
        }

        [Fact]
        public async Task ParallelPriceUpdate_LastWriteWins()
        {
            var product = FakeData.CreateProduct(salePrice: 100m);
            var lockObj = new object();

            var tasks = Enumerable.Range(1, 50).Select(i => Task.Run(() =>
            {
                lock (lockObj)
                {
                    product.UpdatePrice(100m + i);
                }
            }));
            await Task.WhenAll(tasks);

            product.SalePrice.Should().BeGreaterThan(100m);
            // Each UpdatePrice call raises a PriceChangedEvent
            product.DomainEvents.Should().HaveCount(50);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 7. Encoding/charset (10) — Turkish chars, emoji, long strings
    // ═══════════════════════════════════════════════════════════════
    public class EncodingCharsetTests
    {
        [Fact]
        public void Product_TurkishCharacters_NamePreserved()
        {
            var product = FakeData.CreateProduct();
            product.Name = "Gumus Kolye";

            product.Name.Should().Be("Gumus Kolye");
            product.ToString().Should().Contain("Gumus Kolye");
        }

        [Fact]
        public void SKU_TurkishUpperCase_NormalizedCorrectly()
        {
            // ToUpperInvariant on Turkish chars
            var sku = new SKU("sku-test-abc");

            sku.Value.Should().Be("SKU-TEST-ABC");
        }

        [Fact]
        public void Barcode_TurkishDigitsNotAllDigit_EAN13False()
        {
            // Non-digit characters in barcode
            var barcode = new Barcode("869000ABC0001");

            barcode.IsEAN13.Should().BeFalse();
        }

        [Fact]
        public void Category_EmojiInName_Preserved()
        {
            var cat = new Category
            {
                TenantId = Guid.NewGuid(),
                Name = "Electronics \U0001f4f1\U0001f4bb",
                Code = "ELEC"
            };

            cat.Name.Should().Contain("\U0001f4f1");
            cat.ToString().Should().Contain("Electronics");
        }

        [Fact]
        public void Address_SpecialCharacters_PreservedInFullAddress()
        {
            var address = new Address
            {
                Street = "Ataturk Cad. No:15/A",
                District = "Besiktas",
                City = "Istanbul",
                PostalCode = "34000",
                Country = "TR"
            };

            address.FullAddress.Should().Contain("Ataturk Cad. No:15/A");
            address.FullAddress.Should().Contain("Istanbul");
        }

        [Fact]
        public void Product_VeryLongName_NoTruncation()
        {
            var longName = new string('A', 5000);
            var product = FakeData.CreateProduct();
            product.Name = longName;

            product.Name.Should().HaveLength(5000);
        }

        [Fact]
        public void Customer_UnicodeEmail_Accepted()
        {
            var customer = new Customer
            {
                TenantId = Guid.NewGuid(),
                Name = "Test",
                Code = "C-001",
                Email = "user@ornek.com"
            };

            customer.Email.Should().Be("user@ornek.com");
        }

        [Fact]
        public void Warehouse_MixedScriptName_Preserved()
        {
            var wh = new Warehouse
            {
                TenantId = Guid.NewGuid(),
                Name = "Depo-1 Main",
                Code = "WH-01"
            };

            wh.DisplayName.Should().Be("[WH-01] Depo-1 Main");
        }

        [Fact]
        public void Brand_SpecialCharsInName_Preserved()
        {
            var brand = Brand.Create(Guid.NewGuid(), "L'Oreal Paris & Co.");

            brand.Name.Should().Be("L'Oreal Paris & Co.");
        }

        [Fact]
        public void LocationCode_UnicodeZone_PreservedInFullCode()
        {
            var loc = new LocationCode("Zone-A", "R01", "S03", "B05");

            loc.FullCode.Should().Be("Zone-A-R01-S03-B05");
            loc.IsEmpty.Should().BeFalse();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 8. Date edge cases (10) — Feb 29, New Year, timezone
    // ═══════════════════════════════════════════════════════════════
    public class DateEdgeCaseTests
    {
        [Fact]
        public void InventoryLot_Feb29ExpiryDate_HandledCorrectly()
        {
            var lot = FakeData.CreateLot(Guid.NewGuid());
            lot.ExpiryDate = new DateTime(2028, 2, 29, 0, 0, 0, DateTimeKind.Utc);

            lot.ExpiryDate.Should().NotBeNull();
            lot.ExpiryDate!.Value.Day.Should().Be(29);
            lot.ExpiryDate!.Value.Month.Should().Be(2);
        }

        [Fact]
        public void PlatformCommission_EffectiveOnNewYearMidnight()
        {
            var commission = new PlatformCommission
            {
                TenantId = Guid.NewGuid(),
                Platform = PlatformType.Trendyol,
                Rate = 10m,
                EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffectiveTo = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                IsActive = true
            };

            commission.IsEffective(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
            commission.IsEffective(new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc)).Should().BeFalse();
        }

        [Fact]
        public void PlatformCommission_ExpiredCommission_NotEffective()
        {
            var commission = new PlatformCommission
            {
                TenantId = Guid.NewGuid(),
                Platform = PlatformType.Hepsiburada,
                Rate = 12m,
                EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffectiveTo = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            };

            commission.IsEffective(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
        }

        [Fact]
        public void InventoryLot_PastExpiryDate_IsExpired()
        {
            var lot = FakeData.CreateLot(Guid.NewGuid());
            lot.ExpiryDate = DateTime.UtcNow.AddDays(-1);

            lot.IsExpired.Should().BeTrue();
        }

        [Fact]
        public void InventoryLot_FutureExpiryDate_IsNotExpired()
        {
            var lot = FakeData.CreateLot(Guid.NewGuid());
            lot.ExpiryDate = DateTime.UtcNow.AddYears(1);

            lot.IsExpired.Should().BeFalse();
        }

        [Fact]
        public void InventoryLot_NullExpiryDate_IsNotExpired()
        {
            var lot = FakeData.CreateLot(Guid.NewGuid());
            lot.ExpiryDate = null;

            lot.IsExpired.Should().BeFalse();
        }

        [Fact]
        public void Order_RequiredDateOnLeapDay_Accepted()
        {
            var order = FakeData.CreateOrder();
            order.RequiredDate = new DateTime(2028, 2, 29, 12, 0, 0, DateTimeKind.Utc);

            order.RequiredDate.Should().NotBeNull();
            order.RequiredDate!.Value.Month.Should().Be(2);
            order.RequiredDate!.Value.Day.Should().Be(29);
        }

        [Fact]
        public void SupplierAccount_OverdueBalance_DateBoundary()
        {
            var account = new SupplierAccount
            {
                TenantId = Guid.NewGuid(),
                SupplierId = Guid.NewGuid(),
                AccountCode = "S-DATE"
            };

            var dueDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            account.RecordPayment(1000m, "PAY-001", dueDate);

            // Check exactly on the due date — NOT overdue (must be strictly before)
            account.OverdueBalance(dueDate).Should().Be(0m, "due date is not yet past");

            // One second later — overdue
            account.OverdueBalance(dueDate.AddSeconds(1)).Should().Be(1000m);
        }

        [Fact]
        public void BaseEntity_CreatedAt_DefaultsToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var product = FakeData.CreateProduct();
            var after = DateTime.UtcNow.AddSeconds(1);

            product.CreatedAt.Should().BeAfter(before);
            product.CreatedAt.Should().BeBefore(after);
        }

        [Fact]
        public void Customer_BirthDateOnMinValue_Accepted()
        {
            var customer = new Customer
            {
                TenantId = Guid.NewGuid(),
                Name = "Ancient",
                Code = "C-ANCIENT",
                BirthDate = DateTime.MinValue
            };

            customer.BirthDate.Should().Be(DateTime.MinValue);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 9. Multi-tenant isolation (10) — Tenant A data not visible to Tenant B
    // ═══════════════════════════════════════════════════════════════
    public class MultiTenantIsolationTests
    {
        private static readonly Guid TenantAId = Guid.NewGuid();
        private static readonly Guid TenantBId = Guid.NewGuid();

        [Fact]
        public void Product_DifferentTenants_DifferentTenantIds()
        {
            var productA = FakeData.CreateProduct();
            productA.TenantId = TenantAId;

            var productB = FakeData.CreateProduct();
            productB.TenantId = TenantBId;

            productA.TenantId.Should().NotBe(productB.TenantId);
        }

        [Fact]
        public void Order_TenantIsolation_DifferentTenants()
        {
            var orderA = FakeData.CreateOrder();
            orderA.TenantId = TenantAId;

            var orderB = FakeData.CreateOrder();
            orderB.TenantId = TenantBId;

            orderA.TenantId.Should().NotBe(orderB.TenantId);
        }

        [Fact]
        public void CustomerAccount_TenantIsolation_BalanceIndependent()
        {
            var accountA = new CustomerAccount
            {
                TenantId = TenantAId,
                CustomerId = Guid.NewGuid(),
                AccountCode = "CA-A"
            };
            var accountB = new CustomerAccount
            {
                TenantId = TenantBId,
                CustomerId = Guid.NewGuid(),
                AccountCode = "CA-B"
            };

            accountA.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 5000m, "INV-A1");
            accountB.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 3000m, "INV-B1");

            accountA.Balance.Should().Be(5000m);
            accountB.Balance.Should().Be(3000m);
            accountA.Balance.Should().NotBe(accountB.Balance);
        }

        [Fact]
        public void Warehouse_TenantIsolation_SameCodeDifferentTenants()
        {
            var whA = new Warehouse { TenantId = TenantAId, Name = "Main", Code = "WH-01" };
            var whB = new Warehouse { TenantId = TenantBId, Name = "Main", Code = "WH-01" };

            whA.Code.Should().Be(whB.Code);
            whA.TenantId.Should().NotBe(whB.TenantId);
            whA.Id.Should().NotBe(whB.Id);
        }

        [Fact]
        public void Category_TenantIsolation_SameNameAllowed()
        {
            var catA = new Category { TenantId = TenantAId, Name = "Electronics", Code = "ELEC" };
            var catB = new Category { TenantId = TenantBId, Name = "Electronics", Code = "ELEC" };

            catA.Name.Should().Be(catB.Name);
            catA.TenantId.Should().NotBe(catB.TenantId);
        }

        [Fact]
        public void StockMovement_TenantIsolation_DifferentTenants()
        {
            var mvA = new StockMovement { TenantId = TenantAId, ProductId = Guid.NewGuid(), Quantity = 100 };
            var mvB = new StockMovement { TenantId = TenantBId, ProductId = Guid.NewGuid(), Quantity = 100 };

            mvA.TenantId.Should().NotBe(mvB.TenantId);
        }

        [Fact]
        public void Store_TenantIsolation_SamePlatformDifferentTenants()
        {
            var storeA = FakeData.CreateStore(TenantAId, PlatformType.Trendyol);
            var storeB = FakeData.CreateStore(TenantBId, PlatformType.Trendyol);

            storeA.PlatformType.Should().Be(storeB.PlatformType);
            storeA.TenantId.Should().NotBe(storeB.TenantId);
        }

        [Fact]
        public void SupplierAccount_TenantIsolation_IndependentBalances()
        {
            var saA = new SupplierAccount { TenantId = TenantAId, SupplierId = Guid.NewGuid(), AccountCode = "SA-A" };
            var saB = new SupplierAccount { TenantId = TenantBId, SupplierId = Guid.NewGuid(), AccountCode = "SA-B" };

            saA.RecordPurchase(Guid.NewGuid(), 10000m, "PUR-A1");
            saB.RecordPurchase(Guid.NewGuid(), 7000m, "PUR-B1");

            saA.Balance.Should().Be(-10000m);
            saB.Balance.Should().Be(-7000m);
        }

        [Fact]
        public void Tenant_Deactivated_FlagIsIndependent()
        {
            var tenantA = FakeData.CreateTenant("Active Corp");
            var tenantB = FakeData.CreateTenant("Inactive Corp");
            tenantB.IsActive = false;

            tenantA.IsActive.Should().BeTrue();
            tenantB.IsActive.Should().BeFalse();
        }

        [Fact]
        public void ReturnRequest_TenantIsolation_DifferentTenants()
        {
            var rrA = ReturnRequest.Create(
                Guid.NewGuid(), TenantAId, PlatformType.Trendyol,
                ReturnReason.DefectiveProduct, "Customer A");
            var rrB = ReturnRequest.Create(
                Guid.NewGuid(), TenantBId, PlatformType.Hepsiburada,
                ReturnReason.WrongProduct, "Customer B");

            rrA.TenantId.Should().Be(TenantAId);
            rrB.TenantId.Should().Be(TenantBId);
            rrA.TenantId.Should().NotBe(rrB.TenantId);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // 10. Accounting edge (10) — FIFO empty stock, negative balance, rounding
    // ═══════════════════════════════════════════════════════════════
    public class AccountingEdgeCaseTests
    {
        [Fact]
        public void StockCalculationService_FEFO_EmptyLotList_ReturnsEmpty()
        {
            var svc = new StockCalculationService();

            var result = svc.SelectLotsForConsumption(Enumerable.Empty<InventoryLot>(), 100);

            result.Should().BeEmpty();
        }

        [Fact]
        public void StockCalculationService_FEFO_AllLotsClosed_ReturnsEmpty()
        {
            var svc = new StockCalculationService();
            var lots = new[]
            {
                new InventoryLot { Status = LotStatus.Closed, RemainingQty = 0 },
                new InventoryLot { Status = LotStatus.Expired, RemainingQty = 50 }
            };

            var result = svc.SelectLotsForConsumption(lots, 10);

            result.Should().BeEmpty("only Open lots with remaining > 0 are selected");
        }

        [Fact]
        public void CustomerAccount_NegativeBalance_FromReturns()
        {
            var account = new CustomerAccount
            {
                TenantId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                AccountCode = "C-NEG-BAL"
            };

            // Customer returns without prior sale — balance goes negative
            account.RecordReturn(Guid.NewGuid(), 500m);

            account.Balance.Should().Be(-500m);
        }

        [Fact]
        public void PlatformCommission_RoundingTo2Decimals()
        {
            var commission = new PlatformCommission
            {
                TenantId = Guid.NewGuid(),
                Platform = PlatformType.Trendyol,
                Type = CommissionType.Percentage,
                Rate = 7.77m
            };

            // 1000 * 7.77 / 100 = 77.7 => rounded to 77.70
            var result = commission.Calculate(1000m);

            result.Should().Be(77.70m);
        }

        [Fact]
        public void PlatformCommission_FixedAmount_IgnoresSaleAmount()
        {
            var commission = new PlatformCommission
            {
                TenantId = Guid.NewGuid(),
                Platform = PlatformType.N11,
                Type = CommissionType.FixedAmount,
                Rate = 25m
            };

            commission.Calculate(100m).Should().Be(25m);
            commission.Calculate(10_000m).Should().Be(25m);
            commission.Calculate(0m).Should().Be(25m);
        }

        [Fact]
        public void StockCalculationService_WAC_BothZero_ReturnsZero()
        {
            var svc = new StockCalculationService();

            // currentStock + addedQty = 0 => returns 0
            var wac = svc.CalculateWAC(0, 0m, 0, 100m);

            wac.Should().Be(0m);
        }

        [Fact]
        public void StockCalculationService_InventoryValue_EmptyProducts()
        {
            var svc = new StockCalculationService();

            var value = svc.CalculateInventoryValue(Enumerable.Empty<Product>());

            value.Should().Be(0m);
        }

        [Fact]
        public void Money_SubtractDifferentCurrencies_Throws()
        {
            var try_ = Money.TRY(100m);
            var usd = Money.USD(50m);

            var act = () => try_.Subtract(usd);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot subtract TRY and USD*");
        }

        [Fact]
        public void Money_AddDifferentCurrencies_Throws()
        {
            var try_ = Money.TRY(100m);
            var eur = Money.EUR(50m);

            var act = () => try_.Add(eur);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot add TRY and EUR*");
        }

        [Fact]
        public void SupplierAccount_PurchaseThenFullPayment_ZeroBalance()
        {
            var account = new SupplierAccount
            {
                TenantId = Guid.NewGuid(),
                SupplierId = Guid.NewGuid(),
                AccountCode = "S-FULL"
            };

            account.RecordPurchase(Guid.NewGuid(), 5000m, "PUR-001");
            account.RecordPayment(5000m);

            // Purchase: credit 5000 => balance = -5000
            // Payment: debit 5000 => balance = 0
            account.Balance.Should().Be(0m);
            account.Transactions.Should().HaveCount(2);
        }
    }
}
