using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using MesTech.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Dashboard;

/// <summary>
/// GetDashboardSummaryHandler integration-unit testleri.
/// EF InMemory + gerçek DashboardSummaryRepository.
/// ⚠️ Doğrulanmış property: Product.Stock, ReturnRequests, Order.OrderDate
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Dashboard")]
public class GetDashboardSummaryHandlerTests : IDisposable
{
    private readonly AppDbContext _ctx;
    private readonly DashboardSummaryRepository _repo;
    private readonly GetDashboardSummaryQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetDashboardSummaryHandlerTests()
    {
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _ctx = new AppDbContext(options, tenantProviderMock.Object);
        _repo = new DashboardSummaryRepository(_ctx);
        _handler = new GetDashboardSummaryQueryHandler(_repo);
    }

    public void Dispose() => _ctx.Dispose();

    // ═══════════════════════════════════════════════════════════
    // 1. Boş DB — tüm sayısal değerler sıfır
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task EmptyDatabase_ReturnsZeros()
    {
        var result = await _handler.Handle(
            new GetDashboardSummaryQuery(_tenantId), CancellationToken.None);

        result.TodayOrderCount.Should().Be(0);
        result.TodaySalesAmount.Should().Be(0);
        result.ActiveProductCount.Should().Be(0);
        result.CriticalStockCount.Should().Be(0);
        result.PendingShipmentCount.Should().Be(0);
        result.MonthlySalesAmount.Should().Be(0);
        result.ReturnRate.Should().Be(0);
        result.Last7DaysSales.Should().BeEmpty();
        result.RecentOrders.Should().BeEmpty();
        result.CriticalStockItems.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════
    // 2. Bugünkü siparişler — doğru sayı ve toplam
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task TodayOrders_CorrectCountAndAmount()
    {
        var today = DateTime.UtcNow.Date;

        _ctx.Orders.AddRange(
            MakeOrder(100m, today.AddHours(2)),    // bugün
            MakeOrder(200m, today.AddHours(5)),    // bugün
            MakeOrder(999m, today.AddDays(-1))     // dün — sayılmamalı
        );
        await _ctx.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetDashboardSummaryQuery(_tenantId), CancellationToken.None);

        result.TodayOrderCount.Should().Be(2);
        result.TodaySalesAmount.Should().Be(300m);
    }

    // ═══════════════════════════════════════════════════════════
    // 3. Kritik stok — Product.Stock <= MinimumStock
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CriticalStock_CountsLowStockProducts()
    {
        _ctx.Products.AddRange(
            MakeProduct(stock: 3, minStock: 5),    // kritik (3 <= 5)
            MakeProduct(stock: 5, minStock: 5),    // kritik (5 <= 5)
            MakeProduct(stock: 6, minStock: 5),    // normal
            MakeProduct(stock: 0, minStock: 5)     // kritik (out of stock)
        );
        await _ctx.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetDashboardSummaryQuery(_tenantId), CancellationToken.None);

        result.CriticalStockCount.Should().Be(3);
    }

    // ═══════════════════════════════════════════════════════════
    // 4. Son 7 gün satış — gün bazında gruplama
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Last7DaysSales_GroupedByDay()
    {
        var today = DateTime.UtcNow.Date;

        _ctx.Orders.AddRange(
            MakeOrder(100m, today.AddDays(-1)),
            MakeOrder(150m, today.AddDays(-1)),    // aynı gün → birleşmeli
            MakeOrder(200m, today.AddDays(-3)),
            MakeOrder(500m, today.AddDays(-10))    // 7 günden eski → dahil edilmemeli
        );
        await _ctx.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetDashboardSummaryQuery(_tenantId), CancellationToken.None);

        result.Last7DaysSales.Should().HaveCount(2);

        var dayMinus1 = result.Last7DaysSales.SingleOrDefault(d => d.Date == today.AddDays(-1));
        dayMinus1.Should().NotBeNull();
        dayMinus1!.Amount.Should().Be(250m);
        dayMinus1.OrderCount.Should().Be(2);
    }

    // ═══════════════════════════════════════════════════════════
    // 5. İade oranı — ReturnRequests kullan
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ReturnRate_CalculatedCorrectly()
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // 4 sipariş bu ay
        _ctx.Orders.AddRange(
            MakeOrder(100m, monthStart.AddDays(1)),
            MakeOrder(100m, monthStart.AddDays(2)),
            MakeOrder(100m, monthStart.AddDays(3)),
            MakeOrder(100m, monthStart.AddDays(4))
        );

        // 1 iade bu ay → oran %25
        _ctx.ReturnRequests.Add(new ReturnRequest
        {
            TenantId = _tenantId,
            CreatedAt = monthStart.AddDays(2),
            IsDeleted = false
        });

        await _ctx.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetDashboardSummaryQuery(_tenantId), CancellationToken.None);

        result.ReturnRate.Should().Be(25.0m);
    }

    // ─── Yardımcılar ──────────────────────────────────────────

    private Order MakeOrder(decimal amount, DateTime orderDate) => new()
    {
        TenantId = _tenantId,
        OrderDate = orderDate,
        TotalAmount = amount,
        Status = OrderStatus.Pending,
        IsDeleted = false
    };

    private Product MakeProduct(int stock, int minStock) => new()
    {
        TenantId = _tenantId,
        Name = "Ürün-" + Guid.NewGuid().ToString("N")[..8],
        Stock = stock,
        MinimumStock = minStock,
        IsActive = true,
        IsDeleted = false
    };
}
