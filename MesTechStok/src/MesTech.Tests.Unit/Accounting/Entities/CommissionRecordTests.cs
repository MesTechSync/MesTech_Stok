using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class CommissionRecordTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var record = CommissionRecord.Create(
            _tenantId, "Trendyol", 1000m, 0.15m, 150m, 10m,
            "ORD-001", "Elektronik");

        record.Should().NotBeNull();
        record.Platform.Should().Be("Trendyol");
        record.GrossAmount.Should().Be(1000m);
        record.CommissionRate.Should().Be(0.15m);
        record.CommissionAmount.Should().Be(150m);
        record.ServiceFee.Should().Be(10m);
        record.OrderId.Should().Be("ORD-001");
        record.Category.Should().Be("Elektronik");
    }

    [Fact]
    public void Create_ShouldGenerateId()
    {
        var record = CommissionRecord.Create(
            _tenantId, "Trendyol", 1000m, 0.15m, 150m, 0m);

        record.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var record = CommissionRecord.Create(
            _tenantId, "Trendyol", 1000m, 0.15m, 150m, 0m);

        record.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyPlatform_ShouldThrow()
    {
        var act = () => CommissionRecord.Create(
            _tenantId, "", 1000m, 0.15m, 150m, 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullPlatform_ShouldThrow()
    {
        var act = () => CommissionRecord.Create(
            _tenantId, null!, 1000m, 0.15m, 150m, 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithZeroRate_ShouldSucceed()
    {
        var record = CommissionRecord.Create(
            _tenantId, "Trendyol", 1000m, 0m, 0m, 0m);

        record.CommissionRate.Should().Be(0m);
        record.CommissionAmount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithZeroGrossAmount_ShouldSucceed()
    {
        var record = CommissionRecord.Create(
            _tenantId, "Trendyol", 0m, 0.15m, 0m, 0m);

        record.GrossAmount.Should().Be(0m);
    }

    [Fact]
    public void Create_CommissionAmountShouldEqual_GrossTimesRate()
    {
        var gross = 1000m;
        var rate = 0.15m;
        var expected = gross * rate;

        var record = CommissionRecord.Create(
            _tenantId, "Trendyol", gross, rate, expected, 0m);

        record.CommissionAmount.Should().Be(150m);
    }

    [Fact]
    public void Create_WithNullOrderId_ShouldAllowNull()
    {
        var record = CommissionRecord.Create(
            _tenantId, "Hepsiburada", 500m, 0.18m, 90m, 0m);

        record.OrderId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullCategory_ShouldAllowNull()
    {
        var record = CommissionRecord.Create(
            _tenantId, "Amazon", 500m, 0.15m, 75m, 0m, "ORD-001");

        record.Category.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var tenantId = Guid.NewGuid();
        var record = CommissionRecord.Create(
            tenantId, "Trendyol", 1000m, 0.15m, 150m, 0m);

        record.TenantId.Should().Be(tenantId);
    }

    [Theory]
    [InlineData("Trendyol")]
    [InlineData("Hepsiburada")]
    [InlineData("N11")]
    [InlineData("Amazon")]
    [InlineData("Ciceksepeti")]
    [InlineData("Pazarama")]
    public void Create_WithDifferentPlatforms_ShouldSucceed(string platform)
    {
        var record = CommissionRecord.Create(
            _tenantId, platform, 1000m, 0.15m, 150m, 0m);

        record.Platform.Should().Be(platform);
    }

    [Fact]
    public void Create_WithServiceFee_ShouldSetCorrectly()
    {
        var record = CommissionRecord.Create(
            _tenantId, "Trendyol", 1000m, 0.15m, 150m, 25m);

        record.ServiceFee.Should().Be(25m);
    }

    [Fact]
    public void Create_WithLargeAmounts_ShouldHandleCorrectly()
    {
        var record = CommissionRecord.Create(
            _tenantId, "Amazon", 999_999m, 0.15m, 149_999.85m, 1000m);

        record.GrossAmount.Should().Be(999_999m);
    }

    [Fact]
    public void Create_MultipleRecords_ShouldHaveUniqueIds()
    {
        var r1 = CommissionRecord.Create(_tenantId, "Trendyol", 100m, 0.15m, 15m, 0m);
        var r2 = CommissionRecord.Create(_tenantId, "Trendyol", 200m, 0.15m, 30m, 0m);

        r1.Id.Should().NotBe(r2.Id);
    }
}
