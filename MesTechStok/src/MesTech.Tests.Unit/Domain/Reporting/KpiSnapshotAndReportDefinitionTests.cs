using FluentAssertions;
using MesTech.Domain.Entities.Reporting;

namespace MesTech.Tests.Unit.Domain.Reporting;

/// <summary>
/// Sprint 2 — KpiSnapshot + ReportDefinition entity testleri.
/// ChangePercent hesaplama, Create, CRUD, edge cases.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Reporting")]
public class KpiSnapshotAndReportDefinitionTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // ══════════════════════════════════════
    // KpiSnapshot
    // ══════════════════════════════════════

    [Fact]
    public void Kpi_Create_Valid_ShouldSucceed()
    {
        var kpi = KpiSnapshot.Create(TenantId, new DateTime(2026, 4, 1),
            KpiType.TotalRevenue, 150000m, 120000m, "Trendyol");

        kpi.Value.Should().Be(150000m);
        kpi.PreviousValue.Should().Be(120000m);
        kpi.PlatformCode.Should().Be("Trendyol");
        kpi.Type.Should().Be(KpiType.TotalRevenue);
    }

    [Fact]
    public void Kpi_ChangePercent_Increase_ShouldBePositive()
    {
        var kpi = KpiSnapshot.Create(TenantId, DateTime.UtcNow,
            KpiType.TotalOrders, 150m, 100m);

        kpi.ChangePercent.Should().Be(50m, "(150-100)/100 * 100 = 50%");
    }

    [Fact]
    public void Kpi_ChangePercent_Decrease_ShouldBeNegative()
    {
        var kpi = KpiSnapshot.Create(TenantId, DateTime.UtcNow,
            KpiType.TotalOrders, 80m, 100m);

        kpi.ChangePercent.Should().Be(-20m, "(80-100)/100 * 100 = -20%");
    }

    [Fact]
    public void Kpi_ChangePercent_NoPrevious_ShouldBeZero()
    {
        var kpi = KpiSnapshot.Create(TenantId, DateTime.UtcNow,
            KpiType.AverageOrderValue, 250m);

        kpi.ChangePercent.Should().Be(0m);
    }

    [Fact]
    public void Kpi_ChangePercent_PreviousZero_ShouldBeZero()
    {
        var kpi = KpiSnapshot.Create(TenantId, DateTime.UtcNow,
            KpiType.NewCustomers, 10m, 0m);

        kpi.ChangePercent.Should().Be(0m, "division by zero → 0%");
    }

    [Fact]
    public void Kpi_ChangePercent_DoubledValue_Should100Percent()
    {
        var kpi = KpiSnapshot.Create(TenantId, DateTime.UtcNow,
            KpiType.TotalRevenue, 200m, 100m);

        kpi.ChangePercent.Should().Be(100m);
    }

    [Fact]
    public void Kpi_Create_EmptyTenantId_ShouldThrow()
    {
        var act = () => KpiSnapshot.Create(Guid.Empty, DateTime.UtcNow, KpiType.TotalRevenue, 100m);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(KpiType.TotalRevenue)]
    [InlineData(KpiType.OutOfStock)]
    [InlineData(KpiType.ReturnRate)]
    [InlineData(KpiType.CommissionTotal)]
    [InlineData(KpiType.SettlementPending)]
    public void Kpi_AllTypes_ShouldCreate(KpiType type)
    {
        var kpi = KpiSnapshot.Create(TenantId, DateTime.UtcNow, type, 42m);
        kpi.Type.Should().Be(type);
    }

    // ══════════════════════════════════════
    // ReportDefinition
    // ══════════════════════════════════════

    [Fact]
    public void Report_Create_Valid_ShouldSucceed()
    {
        var r = ReportDefinition.Create(TenantId, "Aylık Satış Raporu",
            ReportType.SalesByPlatform, ReportFrequency.Monthly, "admin@mestech.com");

        r.Name.Should().Be("Aylık Satış Raporu");
        r.Type.Should().Be(ReportType.SalesByPlatform);
        r.Frequency.Should().Be(ReportFrequency.Monthly);
        r.RecipientEmail.Should().Be("admin@mestech.com");
        r.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Report_Create_EmptyName_ShouldThrow()
    {
        var act = () => ReportDefinition.Create(TenantId, "", ReportType.StockStatus);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Report_Create_EmptyTenantId_ShouldThrow()
    {
        var act = () => ReportDefinition.Create(Guid.Empty, "Test", ReportType.StockABC);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Report_Create_DefaultOnDemand()
    {
        var r = ReportDefinition.Create(TenantId, "Quick Report", ReportType.OrderStatus);
        r.Frequency.Should().Be(ReportFrequency.OnDemand);
    }

    [Fact]
    public void Report_SetFilter_ShouldStoreJson()
    {
        var r = ReportDefinition.Create(TenantId, "Filtered", ReportType.SalesByCategory);
        r.SetFilter("{\"from\":\"2026-01-01\",\"to\":\"2026-03-31\",\"platform\":\"Trendyol\"}");

        r.FilterJson.Should().Contain("Trendyol");
    }

    [Fact]
    public void Report_Deactivate_ShouldSetInactive()
    {
        var r = ReportDefinition.Create(TenantId, "Deact Test", ReportType.ProfitLoss);
        r.IsActive.Should().BeTrue();
        r.Deactivate();

        r.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(ReportType.SalesByPlatform)]
    [InlineData(ReportType.StockABC)]
    [InlineData(ReportType.CommissionComparison)]
    [InlineData(ReportType.CustomerLifetimeValue)]
    [InlineData(ReportType.CargoPerformance)]
    public void Report_AllTypes_ShouldCreate(ReportType type)
    {
        var r = ReportDefinition.Create(TenantId, $"Report {type}", type);
        r.Type.Should().Be(type);
    }
}
