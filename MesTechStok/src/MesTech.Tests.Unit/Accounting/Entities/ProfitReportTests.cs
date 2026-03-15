using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class ProfitReportTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var report = ProfitReport.Create(
            _tenantId, DateTime.UtcNow, "2026-03",
            10000m, 6000m, 1500m, 500m, 200m, "Trendyol");

        report.Should().NotBeNull();
        report.Period.Should().Be("2026-03");
        report.Platform.Should().Be("Trendyol");
        report.TotalRevenue.Should().Be(10000m);
    }

    [Fact]
    public void Create_NetProfit_ShouldBeCalculated()
    {
        // NetProfit = Revenue - Cost - Commission - Cargo - Tax
        var report = ProfitReport.Create(
            _tenantId, DateTime.UtcNow, "2026-03",
            10000m, 6000m, 1500m, 500m, 200m);

        report.NetProfit.Should().Be(1800m); // 10000 - 6000 - 1500 - 500 - 200
    }

    [Fact]
    public void Create_WithEmptyPeriod_ShouldThrow()
    {
        var act = () => ProfitReport.Create(
            _tenantId, DateTime.UtcNow, "",
            1000m, 500m, 100m, 50m, 20m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullPlatform_ShouldAllowNull()
    {
        var report = ProfitReport.Create(
            _tenantId, DateTime.UtcNow, "2026-03",
            1000m, 500m, 100m, 50m, 20m);

        report.Platform.Should().BeNull();
    }

    [Fact]
    public void Create_WithZeroRevenue_ShouldSucceed()
    {
        var report = ProfitReport.Create(
            _tenantId, DateTime.UtcNow, "2026-03",
            0m, 0m, 0m, 0m, 0m);

        report.NetProfit.Should().Be(0m);
    }

    [Fact]
    public void Create_NetProfit_CanBeNegative()
    {
        var report = ProfitReport.Create(
            _tenantId, DateTime.UtcNow, "2026-03",
            1000m, 800m, 150m, 100m, 50m);

        // 1000 - 800 - 150 - 100 - 50 = -100
        report.NetProfit.Should().Be(-100m);
    }

    [Fact]
    public void Create_ShouldSetReportDate()
    {
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var report = ProfitReport.Create(
            _tenantId, date, "2026-03",
            1000m, 500m, 100m, 50m, 20m);

        report.ReportDate.Should().Be(date);
    }

    [Fact]
    public void Create_ShouldSetAllFinancialFields()
    {
        var report = ProfitReport.Create(
            _tenantId, DateTime.UtcNow, "2026-03",
            10000m, 6000m, 1500m, 500m, 200m);

        report.TotalRevenue.Should().Be(10000m);
        report.TotalCost.Should().Be(6000m);
        report.TotalCommission.Should().Be(1500m);
        report.TotalCargo.Should().Be(500m);
        report.TotalTax.Should().Be(200m);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var r1 = ProfitReport.Create(_tenantId, DateTime.UtcNow, "2026-01", 1000m, 500m, 100m, 50m, 20m);
        var r2 = ProfitReport.Create(_tenantId, DateTime.UtcNow, "2026-02", 2000m, 1000m, 200m, 100m, 40m);

        r1.Id.Should().NotBe(r2.Id);
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var report = ProfitReport.Create(
            _tenantId, DateTime.UtcNow, "2026-03",
            1000m, 500m, 100m, 50m, 20m);

        report.TenantId.Should().Be(_tenantId);
    }

    [Theory]
    [InlineData(10000, 6000, 1500, 500, 200, 1800)]
    [InlineData(5000, 3000, 750, 250, 100, 900)]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(1000, 1500, 0, 0, 0, -500)]
    public void Create_NetProfitCalculation_ShouldBeCorrect(
        decimal revenue, decimal cost, decimal commission,
        decimal cargo, decimal tax, decimal expectedNetProfit)
    {
        var report = ProfitReport.Create(
            _tenantId, DateTime.UtcNow, "2026-03",
            revenue, cost, commission, cargo, tax);

        report.NetProfit.Should().Be(expectedNetProfit);
    }
}
