using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class SettlementLineTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _batchId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-001",
            1000m, 150m, 10m, 30m, 0m, 810m);

        line.Should().NotBeNull();
        line.TenantId.Should().Be(_tenantId);
        line.SettlementBatchId.Should().Be(_batchId);
        line.OrderId.Should().Be("ORD-001");
        line.GrossAmount.Should().Be(1000m);
        line.CommissionAmount.Should().Be(150m);
        line.ServiceFee.Should().Be(10m);
        line.CargoDeduction.Should().Be(30m);
        line.RefundDeduction.Should().Be(0m);
        line.NetAmount.Should().Be(810m);
    }

    [Fact]
    public void Create_ShouldSetId()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-001",
            500m, 75m, 0m, 0m, 0m, 425m);

        line.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-001",
            500m, 75m, 0m, 0m, 0m, 425m);

        line.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullOrderId_ShouldAllowNull()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, null,
            500m, 75m, 0m, 0m, 0m, 425m);

        line.OrderId.Should().BeNull();
    }

    [Fact]
    public void Create_WithZeroAmounts_ShouldSucceed()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-002",
            0m, 0m, 0m, 0m, 0m, 0m);

        line.GrossAmount.Should().Be(0m);
        line.NetAmount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithAllDeductions_ShouldStoreValues()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-003",
            1000m, 150m, 20m, 50m, 100m, 680m);

        line.ServiceFee.Should().Be(20m);
        line.CargoDeduction.Should().Be(50m);
        line.RefundDeduction.Should().Be(100m);
    }

    [Fact]
    public void Create_NetAmountShouldMatch_GrossMinusDeductions()
    {
        // Business rule: net = gross - commission - serviceFee - cargo - refund
        decimal gross = 1000m;
        decimal commission = 150m;
        decimal serviceFee = 10m;
        decimal cargo = 30m;
        decimal refund = 0m;
        decimal net = gross - commission - serviceFee - cargo - refund;

        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-004",
            gross, commission, serviceFee, cargo, refund, net);

        line.NetAmount.Should().Be(810m);
    }

    [Fact]
    public void Create_WithLargeAmounts_ShouldHandleCorrectly()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-BIG",
            999_999.99m, 149_999.99m, 1000m, 5000m, 0m, 844_000m);

        line.GrossAmount.Should().Be(999_999.99m);
    }

    [Fact]
    public void Create_WithDecimalPrecision_ShouldPreserve()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-DEC",
            123.45m, 18.52m, 1.23m, 4.56m, 0m, 99.14m);

        line.CommissionAmount.Should().Be(18.52m);
    }

    [Fact]
    public void Create_MultipleLines_ShouldHaveUniqueIds()
    {
        var line1 = SettlementLine.Create(_tenantId, _batchId, "ORD-1", 100m, 15m, 0m, 0m, 0m, 85m);
        var line2 = SettlementLine.Create(_tenantId, _batchId, "ORD-2", 200m, 30m, 0m, 0m, 0m, 170m);

        line1.Id.Should().NotBe(line2.Id);
    }

    [Fact]
    public void Create_NavigationProperty_ShouldBeNull()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-001",
            500m, 75m, 0m, 0m, 0m, 425m);

        line.SettlementBatch.Should().BeNull();
    }

    [Fact]
    public void Create_WithNegativeGross_ShouldStoreValue()
    {
        // The entity does not validate negative values; business layer does
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-REF",
            -500m, 0m, 0m, 0m, 0m, -500m);

        line.GrossAmount.Should().Be(-500m);
    }

    [Fact]
    public void Create_ShouldSetSettlementBatchId()
    {
        var batchId = Guid.NewGuid();
        var line = SettlementLine.Create(_tenantId, batchId, "ORD-X", 100m, 10m, 0m, 0m, 0m, 90m);

        line.SettlementBatchId.Should().Be(batchId);
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var tenantId = Guid.NewGuid();
        var line = SettlementLine.Create(tenantId, _batchId, "ORD-T", 100m, 10m, 0m, 0m, 0m, 90m);

        line.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithRefundDeduction_ShouldSetCorrectly()
    {
        var line = SettlementLine.Create(
            _tenantId, _batchId, "ORD-REF",
            1000m, 150m, 0m, 0m, 200m, 650m);

        line.RefundDeduction.Should().Be(200m);
    }
}
