using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Events;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class TaxWithholdingTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var tw = TaxWithholding.Create(_tenantId, 1000m, 0.01m, "Tevkifat");

        tw.Should().NotBeNull();
        tw.TenantId.Should().Be(_tenantId);
        tw.TaxExclusiveAmount.Should().Be(1000m);
        tw.Rate.Should().Be(0.01m);
        tw.TaxType.Should().Be("Tevkifat");
    }

    [Theory]
    [InlineData(1000, 0.01, 10)]
    [InlineData(2000, 0.05, 100)]
    [InlineData(5000, 0.10, 500)]
    [InlineData(10000, 0.20, 2000)]
    [InlineData(800, 0.0, 0)]
    [InlineData(1000, 1.0, 1000)]
    public void Create_WithholdingAmount_ShouldEqual_MatrahTimesRate(
        decimal matrah, decimal rate, decimal expectedWithholding)
    {
        var tw = TaxWithholding.Create(_tenantId, matrah, rate, "Stopaj");

        tw.WithholdingAmount.Should().Be(expectedWithholding);
    }

    [Fact]
    public void Create_ShouldRaiseTaxWithholdingComputedEvent()
    {
        var tw = TaxWithholding.Create(_tenantId, 1000m, 0.05m, "Stopaj");

        tw.DomainEvents.Should().ContainSingle(e => e is TaxWithholdingComputedEvent);
        var evt = tw.DomainEvents.OfType<TaxWithholdingComputedEvent>().Single();
        evt.TenantId.Should().Be(_tenantId);
        evt.TaxExclusiveAmount.Should().Be(1000m);
        evt.Rate.Should().Be(0.05m);
        evt.WithholdingAmount.Should().Be(50m);
        evt.TaxType.Should().Be("Stopaj");
    }

    [Fact]
    public void Create_WithEmptyTaxType_ShouldThrow()
    {
        var act = () => TaxWithholding.Create(_tenantId, 1000m, 0.01m, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullTaxType_ShouldThrow()
    {
        var act = () => TaxWithholding.Create(_tenantId, 1000m, 0.01m, null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithRateGreaterThan1_ShouldThrow()
    {
        var act = () => TaxWithholding.Create(_tenantId, 1000m, 1.5m, "Stopaj");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeRate_ShouldThrow()
    {
        var act = () => TaxWithholding.Create(_tenantId, 1000m, -0.01m, "Stopaj");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithZeroRate_ShouldSucceed()
    {
        var tw = TaxWithholding.Create(_tenantId, 1000m, 0m, "NoTax");

        tw.WithholdingAmount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithRate1_ShouldSucceed()
    {
        var tw = TaxWithholding.Create(_tenantId, 1000m, 1.0m, "FullTax");

        tw.WithholdingAmount.Should().Be(1000m);
    }

    [Fact]
    public void Create_WithInvoiceId_ShouldSetInvoiceId()
    {
        var invoiceId = Guid.NewGuid();
        var tw = TaxWithholding.Create(_tenantId, 1000m, 0.05m, "Stopaj", invoiceId);

        tw.InvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public void Create_WithNullInvoiceId_ShouldAllowNull()
    {
        var tw = TaxWithholding.Create(_tenantId, 1000m, 0.05m, "Stopaj");

        tw.InvoiceId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetId()
    {
        var tw = TaxWithholding.Create(_tenantId, 1000m, 0.05m, "Stopaj");

        tw.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var tw = TaxWithholding.Create(_tenantId, 1000m, 0.05m, "Stopaj");

        tw.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_Matrah_ShouldBeKdvHaricTutar()
    {
        // 9284 CB rule: matrah = KDV HARIC tutar
        // Commission and cargo should NOT affect matrah
        decimal kdvDahilTutar = 1200m;
        decimal kdvHaricTutar = 1000m; // 1200 / 1.20
        // Commission = 150, Cargo = 30 — these should NOT reduce matrah

        var tw = TaxWithholding.Create(_tenantId, kdvHaricTutar, 0.05m, "Stopaj");

        // matrah is the KDV exclusive amount, NOT reduced by commission/cargo
        tw.TaxExclusiveAmount.Should().Be(1000m);
        tw.WithholdingAmount.Should().Be(50m);
    }

    [Fact]
    public void Create_CommissionShouldNotAffectBase()
    {
        // Commission of 150 TRY should NOT change the tax base
        decimal matrah = 1000m; // KDV haric tutar
        decimal commission = 150m; // This does NOT reduce matrah

        var tw = TaxWithholding.Create(_tenantId, matrah, 0.05m, "Stopaj");

        tw.TaxExclusiveAmount.Should().Be(1000m);
        tw.WithholdingAmount.Should().Be(50m); // 1000 * 0.05, not (1000-150) * 0.05
    }

    [Fact]
    public void Create_CargoShouldNotAffectBase()
    {
        // Cargo of 30 TRY should NOT change the tax base
        decimal matrah = 1000m; // KDV haric tutar
        decimal cargo = 30m; // This does NOT reduce matrah

        var tw = TaxWithholding.Create(_tenantId, matrah, 0.05m, "Stopaj");

        tw.TaxExclusiveAmount.Should().Be(1000m);
        tw.WithholdingAmount.Should().Be(50m); // 1000 * 0.05, not (1000-30) * 0.05
    }

    [Theory]
    [InlineData("KDV Tevkifati")]
    [InlineData("Gelir Vergisi Stopaji")]
    [InlineData("Damga Vergisi")]
    public void Create_WithDifferentTaxTypes_ShouldSetCorrectly(string taxType)
    {
        var tw = TaxWithholding.Create(_tenantId, 1000m, 0.05m, taxType);

        tw.TaxType.Should().Be(taxType);
    }

    [Fact]
    public void Create_WithZeroMatrah_ShouldSucceed()
    {
        var tw = TaxWithholding.Create(_tenantId, 0m, 0.05m, "Stopaj");

        tw.WithholdingAmount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithNegativeMatrah_ShouldStoreValue()
    {
        // Entity does not validate negative matrah; caller is responsible
        var tw = TaxWithholding.Create(_tenantId, -1000m, 0.05m, "Stopaj");

        tw.TaxExclusiveAmount.Should().Be(-1000m);
        tw.WithholdingAmount.Should().Be(-50m);
    }
}
