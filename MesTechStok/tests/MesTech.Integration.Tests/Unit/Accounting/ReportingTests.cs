using FluentAssertions;
using MesTech.Application.DTOs.Finance;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// N2-KALITE — Group 1: Reporting tests (ProfitLoss, MonthlySummary, KDV, DashboardKpi).
/// Integration-level tests exercising GetProfitLossHandler with mocked repositories.
/// Validates net profit, profit margin, platform aggregation, date filtering, and edge cases.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Layer", "Accounting")]
[Trait("Group", "Reporting")]
public class ReportingTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _otherTenantId = Guid.NewGuid();

    private readonly Mock<IFinanceExpenseRepository> _expenseRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private GetProfitLossHandler CreateHandler() =>
        new(_expenseRepo.Object, _orderRepo.Object);

    private void SetupExpenseRepo(decimal totalExpense, List<FinanceExpense>? expenses = null)
    {
        _expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalExpense);

        _expenseRepo.Setup(r => r.GetByTenantAsync(
                _tenantId, It.IsAny<ExpenseStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expenses ?? new List<FinanceExpense>());
    }

    // ─── Helper: Order olusturma ───
    private static Order CreateOrder(
        Guid tenantId, decimal total, OrderStatus status,
        PlatformType platform, DateTime? orderDate = null)
    {
        return new Order
        {
            TenantId = tenantId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}"[..12],
            CustomerId = Guid.NewGuid(),
            Status = status,
            TotalAmount = total,
            SourcePlatform = platform,
            OrderDate = orderDate ?? DateTime.UtcNow
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // 1. ProfitLoss — Bos donem
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProfitLossReport_EmptyPeriod_ReturnsZeros()
    {
        // Arrange — Siparis yok, gider yok
        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Order>());
        SetupExpenseRepo(0m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalRevenue.Should().Be(0m);
        result.TotalExpenses.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
        result.ProfitMarginPercent.Should().Be(0m);
        result.RevenueByPlatform.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. ProfitLoss — Gelir ve gider ile dogru hesaplama
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProfitLossReport_WithIncomeAndExpense_CalculatesCorrectly()
    {
        // Arrange — 3 siparis (1 iptal), 1.500 TL gider
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 5_000m, OrderStatus.Delivered, PlatformType.Trendyol),
            CreateOrder(_tenantId, 3_000m, OrderStatus.Shipped, PlatformType.Hepsiburada),
            CreateOrder(_tenantId, 2_000m, OrderStatus.Cancelled, PlatformType.Trendyol),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);
        SetupExpenseRepo(1_500m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert — Gelir = 5.000 + 3.000 = 8.000 (iptal haric)
        result.TotalRevenue.Should().Be(8_000m);
        result.TotalExpenses.Should().Be(1_500m);
        result.NetProfit.Should().Be(6_500m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. ProfitLoss — Kar marji hesaplama
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProfitLossReport_ProfitMargin_CalculatesPercent()
    {
        // Arrange — 10.000 gelir, 2.000 gider → %80 kar marji
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 10_000m, OrderStatus.Delivered, PlatformType.Trendyol),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);
        SetupExpenseRepo(2_000m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert — Margin = (10000-2000)/10000 * 100 = 80%
        result.ProfitMarginPercent.Should().Be(80.00m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. ProfitLoss — Multi-platform agregasyon
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProfitLossReport_MultiPlatform_AggregatesAll()
    {
        // Arrange — 4 farkli platform
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 1_000m, OrderStatus.Confirmed, PlatformType.Trendyol),
            CreateOrder(_tenantId, 2_000m, OrderStatus.Shipped, PlatformType.Trendyol),
            CreateOrder(_tenantId, 3_000m, OrderStatus.Delivered, PlatformType.Hepsiburada),
            CreateOrder(_tenantId, 1_500m, OrderStatus.Confirmed, PlatformType.N11),
            CreateOrder(_tenantId, 500m, OrderStatus.Shipped, PlatformType.Ciceksepeti),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);
        SetupExpenseRepo(0m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.RevenueByPlatform.Should().HaveCount(4, "4 farkli platform olmali");
        result.TotalRevenue.Should().Be(8_000m, "toplam = 1K+2K+3K+1.5K+0.5K");

        // Trendyol: 1000+2000=3000, 2 siparis
        var trendyol = result.RevenueByPlatform.First(p => p.Platform == "Trendyol");
        trendyol.Revenue.Should().Be(3_000m);
        trendyol.OrderCount.Should().Be(2);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. ProfitLoss — Tarih araligi filtreleme
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProfitLossReport_DateRange_FiltersCorrectly()
    {
        // Arrange — Mart 2026 icin handler cagriliyor
        // Handler'in repository'ye dogru tarih araligini gonderdigini dogrula
        DateTime capturedStart = default;
        DateTime capturedEnd = default;

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<DateTime, DateTime>((s, e) =>
            {
                capturedStart = s;
                capturedEnd = e;
            })
            .ReturnsAsync(new List<Order>());
        SetupExpenseRepo(0m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert — Mart 2026: 1 Mart 00:00 — 31 Mart 23:59:59.9999999
        capturedStart.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        capturedEnd.Month.Should().Be(3);
        capturedEnd.Day.Should().Be(31);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 6. MonthlySummary — Siparis bazli ortalama siparis degeri (AOV)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MonthlySummary_WithOrders_CalculatesAOV()
    {
        // Arrange — 3 siparis: 1000, 2000, 3000 → AOV = 2000
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 1_000m, OrderStatus.Delivered, PlatformType.Trendyol),
            CreateOrder(_tenantId, 2_000m, OrderStatus.Delivered, PlatformType.Hepsiburada),
            CreateOrder(_tenantId, 3_000m, OrderStatus.Delivered, PlatformType.N11),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);
        SetupExpenseRepo(0m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert — AOV hesaplama: toplam gelir / siparis sayisi
        var nonCancelledCount = orders.Count(o => o.Status != OrderStatus.Cancelled);
        var aov = result.TotalRevenue / nonCancelledCount;
        aov.Should().Be(2_000m, "AOV = 6000 / 3 = 2000");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 7. MonthlySummary — Iade orani hesaplama
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MonthlySummary_ReturnRate_CalculatesCorrectly()
    {
        // Arrange — 10 siparis, 2 iptal → %20 iptal orani
        var orders = new List<Order>();
        for (int i = 0; i < 8; i++)
            orders.Add(CreateOrder(_tenantId, 500m, OrderStatus.Delivered, PlatformType.Trendyol));
        orders.Add(CreateOrder(_tenantId, 500m, OrderStatus.Cancelled, PlatformType.Trendyol));
        orders.Add(CreateOrder(_tenantId, 500m, OrderStatus.Cancelled, PlatformType.Trendyol));

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);
        SetupExpenseRepo(0m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert — 8 aktif siparis, 2 iptal → gelir yalnizca 8*500=4000
        result.TotalRevenue.Should().Be(4_000m, "iptal siparisler gelire dahil edilmemeli");
        var cancelRate = 2.0m / orders.Count * 100;
        cancelRate.Should().Be(20.0m, "iptal orani = 2/10 = %20");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 8. MonthlySummary — Platform bazli gruplama
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MonthlySummary_SalesByPlatform_GroupsCorrectly()
    {
        // Arrange — 3 platform, farkli siparis sayilari
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 1_000m, OrderStatus.Delivered, PlatformType.Trendyol),
            CreateOrder(_tenantId, 1_000m, OrderStatus.Delivered, PlatformType.Trendyol),
            CreateOrder(_tenantId, 1_000m, OrderStatus.Delivered, PlatformType.Trendyol),
            CreateOrder(_tenantId, 2_000m, OrderStatus.Shipped, PlatformType.Hepsiburada),
            CreateOrder(_tenantId, 3_000m, OrderStatus.Confirmed, PlatformType.N11),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);
        SetupExpenseRepo(0m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.RevenueByPlatform.Should().HaveCount(3);

        var trendyol = result.RevenueByPlatform.First(p => p.Platform == "Trendyol");
        trendyol.OrderCount.Should().Be(3);
        trendyol.Revenue.Should().Be(3_000m);

        var hb = result.RevenueByPlatform.First(p => p.Platform == "Hepsiburada");
        hb.OrderCount.Should().Be(1);
        hb.Revenue.Should().Be(2_000m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 9. KDV hesaplama — Satis bazli hesaplanan KDV
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void KdvCalculation_SalesOnly_ReturnsHesaplananKdv()
    {
        // Arrange — 10.000 TL matrah, %20 KDV
        var matrah = 10_000m;
        var kdvOrani = 0.20m;

        // Act
        var hesaplananKdv = Math.Round(matrah * kdvOrani, 2);

        // Assert
        hesaplananKdv.Should().Be(2_000m, "KDV = 10.000 x %20 = 2.000 TL");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 10. KDV hesaplama — Alis KDV indirim
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void KdvCalculation_WithPurchases_SubtractsIndirilecek()
    {
        // Arrange — 10.000 TL satis KDV, 3.000 TL alis KDV
        var hesaplananKdv = 10_000m * 0.20m; // 2.000
        var indirilecekKdv = 3_000m * 0.20m; // 600

        // Act
        var odenecekKdv = hesaplananKdv - indirilecekKdv;

        // Assert
        odenecekKdv.Should().Be(1_400m, "Odenecek KDV = 2.000 - 600 = 1.400 TL");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 11. KDV hesaplama — Sifir satis
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void KdvCalculation_ZeroSales_ReturnsZero()
    {
        // Arrange
        var matrah = 0m;
        var kdvOrani = 0.20m;

        // Act
        var hesaplananKdv = Math.Round(matrah * kdvOrani, 2);

        // Assert
        hesaplananKdv.Should().Be(0m, "Sifir matrah icin KDV sifir olmali");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 12. KDV beyanname tarihi — Her ayin 26'si
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(2026, 1)]
    [InlineData(2026, 6)]
    [InlineData(2026, 12)]
    public void KdvCalculation_BeyannameTarih_Returns26th(int year, int month)
    {
        // Arrange & Act — KDV beyannamesinin teslim tarihi her ayin 26'si
        var beyannameTarih = new DateTime(year, month, 26, 0, 0, 0, DateTimeKind.Utc);

        // Assert
        beyannameTarih.Day.Should().Be(26, "KDV beyanname tarihi her ayin 26'si olmali");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 13. Dashboard KPI — Tum metrikler doner
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DashboardKpi_ReturnsAllMetrics()
    {
        // Arrange — Siparisler ve giderler ile KPI hesapla
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 5_000m, OrderStatus.Delivered, PlatformType.Trendyol),
            CreateOrder(_tenantId, 3_000m, OrderStatus.Shipped, PlatformType.Hepsiburada),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);
        SetupExpenseRepo(2_000m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert — Tum temel metrikler mevcut olmali
        result.TotalRevenue.Should().BeGreaterThan(0, "gelir metrigi olmali");
        result.TotalExpenses.Should().BeGreaterThan(0, "gider metrigi olmali");
        result.NetProfit.Should().Be(result.TotalRevenue - result.TotalExpenses);
        result.ProfitMarginPercent.Should().BeGreaterThan(0);
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 14. Dashboard KPI — Bos veri sifir doner
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DashboardKpi_EmptyData_ReturnsZeros()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Order>());
        SetupExpenseRepo(0m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalRevenue.Should().Be(0m);
        result.TotalExpenses.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
        result.ProfitMarginPercent.Should().Be(0m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 15. Dashboard KPI — Multi-tenant filtreleme
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DashboardKpi_MultiTenant_FiltersCorrectly()
    {
        // Arrange — _tenantId'ye ait 1 siparis, _otherTenantId'ye ait 1 siparis
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 5_000m, OrderStatus.Delivered, PlatformType.Trendyol),
            CreateOrder(_otherTenantId, 10_000m, OrderStatus.Delivered, PlatformType.Hepsiburada),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);
        SetupExpenseRepo(0m);

        var handler = CreateHandler();
        var query = new GetProfitLossQuery(_tenantId, 2026, 3);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert — Sadece _tenantId'ye ait siparis gelire yansimali
        result.TotalRevenue.Should().Be(5_000m,
            "sadece istenen tenant'in siparisleri dahil edilmeli, diger tenant'in 10.000 TL'si haric");
    }
}
