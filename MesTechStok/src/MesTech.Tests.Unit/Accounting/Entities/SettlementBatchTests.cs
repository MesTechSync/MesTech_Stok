using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class SettlementBatchTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol",
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
            10000m, 1500m, 8500m);

        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Trendyol");
        batch.TotalGross.Should().Be(10000m);
        batch.TotalCommission.Should().Be(1500m);
        batch.TotalNet.Should().Be(8500m);
    }

    [Fact]
    public void Create_ShouldSetStatusToImported()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 150m, 850m);

        batch.Status.Should().Be(SettlementStatus.Imported);
    }

    [Fact]
    public void Create_ShouldSetImportedAt()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Hepsiburada",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 180m, 820m);

        batch.ImportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldRaiseSettlementImportedEvent()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Amazon",
            DateTime.UtcNow.AddDays(-14), DateTime.UtcNow,
            5000m, 750m, 4250m);

        batch.DomainEvents.Should().ContainSingle(e => e is SettlementImportedEvent);
        var evt = batch.DomainEvents.OfType<SettlementImportedEvent>().Single();
        evt.TenantId.Should().Be(_tenantId);
        evt.Platform.Should().Be("Amazon");
        evt.TotalNet.Should().Be(4250m);
    }

    [Fact]
    public void Create_WithEmptyPlatform_ShouldThrow()
    {
        var act = () => SettlementBatch.Create(
            _tenantId, "",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 100m, 900m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullPlatform_ShouldThrow()
    {
        var act = () => SettlementBatch.Create(
            _tenantId, null!,
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 100m, 900m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddLine_ShouldAddToLines()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 150m, 850m);

        var line = SettlementLine.Create(
            _tenantId, batch.Id, "ORD-001",
            500m, 75m, 10m, 20m, 0m, 395m);

        batch.AddLine(line);

        batch.Lines.Should().HaveCount(1);
    }

    [Fact]
    public void AddLine_MultipleTimes_ShouldAccumulate()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 150m, 850m);

        batch.AddLine(SettlementLine.Create(_tenantId, batch.Id, "ORD-001", 500m, 75m, 0m, 0m, 0m, 425m));
        batch.AddLine(SettlementLine.Create(_tenantId, batch.Id, "ORD-002", 500m, 75m, 0m, 0m, 0m, 425m));

        batch.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void MarkReconciled_ShouldSetStatusToReconciled()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 150m, 850m);

        batch.MarkReconciled();

        batch.Status.Should().Be(SettlementStatus.Reconciled);
    }

    [Fact]
    public void MarkReconciled_ShouldUpdateUpdatedAt()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 150m, 850m);

        batch.MarkReconciled();

        batch.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkDisputed_ShouldSetStatusToDisputed()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Hepsiburada",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 180m, 820m);

        batch.MarkDisputed();

        batch.Status.Should().Be(SettlementStatus.Disputed);
    }

    [Fact]
    public void MarkDisputed_ShouldUpdateUpdatedAt()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Hepsiburada",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 180m, 820m);

        batch.MarkDisputed();

        batch.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldSetPeriodDates()
    {
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol", start, end,
            1000m, 150m, 850m);

        batch.PeriodStart.Should().Be(start);
        batch.PeriodEnd.Should().Be(end);
    }

    [Fact]
    public void Lines_ShouldBeReadOnly()
    {
        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow,
            1000m, 150m, 850m);

        batch.Lines.Should().BeAssignableTo<IReadOnlyList<SettlementLine>>();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var batch1 = SettlementBatch.Create(_tenantId, "Trendyol", DateTime.UtcNow, DateTime.UtcNow, 100m, 10m, 90m);
        var batch2 = SettlementBatch.Create(_tenantId, "Trendyol", DateTime.UtcNow, DateTime.UtcNow, 100m, 10m, 90m);

        batch1.Id.Should().NotBe(batch2.Id);
    }
}
