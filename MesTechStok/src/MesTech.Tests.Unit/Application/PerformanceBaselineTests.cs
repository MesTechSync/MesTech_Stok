using System.Diagnostics;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// Performans baseline testleri.
/// Domain katmaninin toplu islemlerde kabul edilebilir sureler
/// icinde calistigini dogrular. CI/CD pipeline'inda regresyon tespiti saglar.
/// </summary>
[Trait("Category", "Unit")]
public class PerformanceBaselineTests
{
    [Fact]
    public void Creating_10000_Products_ShouldCompleteUnder500ms()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var sw = Stopwatch.StartNew();

        // Act
        var products = new List<Product>(10_000);
        for (int i = 0; i < 10_000; i++)
        {
            products.Add(new Product
            {
                TenantId = tenantId,
                CategoryId = categoryId,
                Name = $"Product-{i}",
                SKU = $"SKU-{i:D5}",
                Barcode = $"869000{i:D7}",
                PurchasePrice = 10m + (i % 100),
                SalePrice = 20m + (i % 100),
                Stock = 100 + (i % 500),
                TaxRate = 0.18m,
                IsActive = true
            });
        }

        sw.Stop();

        // Assert
        products.Should().HaveCount(10_000);
        sw.ElapsedMilliseconds.Should().BeLessThan(500,
            $"10000 Product olusturma {sw.ElapsedMilliseconds}ms surdu, limit 500ms");
    }

    [Fact]
    public void Creating_1000_Orders_WithPlace_ShouldCompleteUnder500ms()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var sw = Stopwatch.StartNew();

        // Act
        var orders = new List<Order>(1_000);
        for (int i = 0; i < 1_000; i++)
        {
            var order = new Order
            {
                TenantId = tenantId,
                OrderNumber = $"ORD-{i:D4}",
                CustomerId = customerId,
                CustomerName = "Perf Test Musteri",
                Status = OrderStatus.Pending
            };

            order.AddItem(new OrderItem
            {
                TenantId = tenantId,
                ProductId = Guid.NewGuid(),
                ProductName = $"Item-{i}",
                ProductSKU = $"SKU-{i:D4}",
                Quantity = 2,
                UnitPrice = 100m,
                TotalPrice = 200m,
                TaxRate = 0.18m,
                TaxAmount = 36m
            });

            order.Place();
            orders.Add(order);
        }

        sw.Stop();

        // Assert
        orders.Should().HaveCount(1_000);
        orders.Should().OnlyContain(o => o.Status == OrderStatus.Confirmed);
        orders.Should().OnlyContain(o => o.DomainEvents.Count == 1);
        sw.ElapsedMilliseconds.Should().BeLessThan(500,
            $"1000 Order+Place islemi {sw.ElapsedMilliseconds}ms surdu, limit 500ms");
    }

    [Fact]
    public void Creating_1000_Invoices_WithCalculateTotals_ShouldCompleteUnder500ms()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sw = Stopwatch.StartNew();

        // Act
        var invoices = new List<MesTech.Domain.Entities.Invoice>(1_000);
        for (int i = 0; i < 1_000; i++)
        {
            var invoice = new MesTech.Domain.Entities.Invoice
            {
                TenantId = tenantId,
                OrderId = Guid.NewGuid(),
                InvoiceNumber = $"INV-{i:D4}",
                Type = InvoiceType.EFatura,
                CustomerName = "Perf Test Musteri",
                CustomerAddress = "Test Adres",
                Currency = "TRY"
            };

            for (int j = 0; j < 5; j++)
            {
                invoice.AddLine(new InvoiceLine
                {
                    ProductName = $"Kalem-{j}",
                    SKU = $"SKU-{j}",
                    Quantity = 3,
                    UnitPrice = 50m + j,
                    TaxRate = 0.18m,
                    TaxAmount = (50m + j) * 3 * 0.18m,
                    DiscountAmount = 0m
                });
            }

            invoices.Add(invoice);
        }

        sw.Stop();

        // Assert
        invoices.Should().HaveCount(1_000);
        invoices.Should().OnlyContain(inv => inv.GrandTotal > 0);
        invoices.Should().OnlyContain(inv => inv.Lines.Count == 5);
        sw.ElapsedMilliseconds.Should().BeLessThan(500,
            $"1000 Invoice+CalculateTotals islemi {sw.ElapsedMilliseconds}ms surdu, limit 500ms");
    }

    [Fact]
    public void DomainEventAccumulation_10000Events_ShouldNotDegrade()
    {
        // Arrange
        var product = new Product
        {
            TenantId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Name = "Event-Stress-Product",
            SKU = "EVT-STRESS",
            PurchasePrice = 10m,
            SalePrice = 20m,
            Stock = 50_000
        };

        var sw = Stopwatch.StartNew();

        // Act — raise 10000 StockChangedEvents via AdjustStock
        for (int i = 0; i < 10_000; i++)
        {
            product.AdjustStock(1, StockMovementType.StockIn, $"batch-{i}");
        }

        sw.Stop();

        // Assert
        product.DomainEvents.Should().HaveCount(10_000);
        product.Stock.Should().Be(60_000); // 50000 + 10000
        sw.ElapsedMilliseconds.Should().BeLessThan(500,
            $"10000 domain event birikimi {sw.ElapsedMilliseconds}ms surdu, limit 500ms");
    }

    [Fact]
    public void GuidGeneration_100000IDs_ShouldCompleteUnder200ms()
    {
        // Arrange
        var sw = Stopwatch.StartNew();

        // Act
        var ids = new List<Guid>(100_000);
        for (int i = 0; i < 100_000; i++)
        {
            ids.Add(Guid.NewGuid());
        }

        sw.Stop();

        // Assert
        ids.Should().HaveCount(100_000);
        ids.Distinct().Should().HaveCount(100_000, "tum GUID'ler benzersiz olmali");
        sw.ElapsedMilliseconds.Should().BeLessThan(200,
            $"100000 Guid.NewGuid() {sw.ElapsedMilliseconds}ms surdu, limit 200ms");
    }
}
