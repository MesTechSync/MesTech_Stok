using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class TaxRecordTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var dueDate = DateTime.UtcNow.AddDays(30);
        var record = TaxRecord.Create(
            _tenantId, "2026-03", "KDV", 10000m, 2000m, dueDate);

        record.Should().NotBeNull();
        record.Period.Should().Be("2026-03");
        record.TaxType.Should().Be("KDV");
        record.TaxableAmount.Should().Be(10000m);
        record.TaxAmount.Should().Be(2000m);
        record.DueDate.Should().Be(dueDate);
    }

    [Fact]
    public void Create_ShouldSetIsPaidToFalse()
    {
        var record = TaxRecord.Create(
            _tenantId, "2026-03", "KDV", 10000m, 2000m, DateTime.UtcNow);

        record.IsPaid.Should().BeFalse();
        record.PaidAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsPaid_ShouldSetIsPaidAndPaidAt()
    {
        var record = TaxRecord.Create(
            _tenantId, "2026-03", "KDV", 10000m, 2000m, DateTime.UtcNow);

        record.MarkAsPaid();

        record.IsPaid.Should().BeTrue();
        record.PaidAt.Should().NotBeNull();
        record.PaidAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsPaid_ShouldUpdateUpdatedAt()
    {
        var record = TaxRecord.Create(
            _tenantId, "2026-03", "KDV", 10000m, 2000m, DateTime.UtcNow);

        record.MarkAsPaid();

        record.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyPeriod_ShouldThrow()
    {
        var act = () => TaxRecord.Create(
            _tenantId, "", "KDV", 10000m, 2000m, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyTaxType_ShouldThrow()
    {
        var act = () => TaxRecord.Create(
            _tenantId, "2026-03", "", 10000m, 2000m, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullPeriod_ShouldThrow()
    {
        var act = () => TaxRecord.Create(
            _tenantId, null!, "KDV", 10000m, 2000m, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullTaxType_ShouldThrow()
    {
        var act = () => TaxRecord.Create(
            _tenantId, "2026-03", null!, 10000m, 2000m, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var r1 = TaxRecord.Create(_tenantId, "2026-01", "KDV", 1000m, 200m, DateTime.UtcNow);
        var r2 = TaxRecord.Create(_tenantId, "2026-02", "KDV", 2000m, 400m, DateTime.UtcNow);

        r1.Id.Should().NotBe(r2.Id);
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var record = TaxRecord.Create(
            _tenantId, "2026-03", "KDV", 10000m, 2000m, DateTime.UtcNow);

        record.TenantId.Should().Be(_tenantId);
    }
}
