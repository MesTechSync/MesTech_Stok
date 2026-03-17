using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class PenaltyRecordTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var penaltyDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.Trendyol,
            "Gec kargo cezasi", 150m, penaltyDate);

        record.Should().NotBeNull();
        record.Source.Should().Be(PenaltySource.Trendyol);
        record.Description.Should().Be("Gec kargo cezasi");
        record.Amount.Should().Be(150m);
        record.PenaltyDate.Should().Be(penaltyDate);
        record.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Create_ShouldSetPaymentStatusToPending()
    {
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.Hepsiburada,
            "Iptal cezasi", 200m, DateTime.UtcNow);

        record.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Create_WithEmptyDescription_ShouldThrow()
    {
        var act = () => PenaltyRecord.Create(
            _tenantId, PenaltySource.Trendyol,
            "", 150m, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullDescription_ShouldThrow()
    {
        var act = () => PenaltyRecord.Create(
            _tenantId, PenaltySource.Trendyol,
            null!, 150m, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldThrow()
    {
        var act = () => PenaltyRecord.Create(
            _tenantId, PenaltySource.Trendyol,
            "Test", 0m, DateTime.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        var act = () => PenaltyRecord.Create(
            _tenantId, PenaltySource.Trendyol,
            "Test", -50m, DateTime.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkAsPaid_ShouldSetStatusToCompleted()
    {
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.N11,
            "Test", 100m, DateTime.UtcNow);

        record.MarkAsPaid();

        record.PaymentStatus.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void MarkAsPaid_ShouldUpdateUpdatedAt()
    {
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.N11,
            "Test", 100m, DateTime.UtcNow);

        record.MarkAsPaid();

        record.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdatePaymentStatus_ShouldSetNewStatus()
    {
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.Amazon,
            "Late shipment", 250m, DateTime.UtcNow);

        record.UpdatePaymentStatus(PaymentStatus.Processing);

        record.PaymentStatus.Should().Be(PaymentStatus.Processing);
    }

    [Theory]
    [InlineData(PenaltySource.Trendyol)]
    [InlineData(PenaltySource.Hepsiburada)]
    [InlineData(PenaltySource.N11)]
    [InlineData(PenaltySource.Ciceksepeti)]
    [InlineData(PenaltySource.Amazon)]
    [InlineData(PenaltySource.eBay)]
    [InlineData(PenaltySource.TaxAuthority)]
    [InlineData(PenaltySource.SGK)]
    [InlineData(PenaltySource.Customs)]
    [InlineData(PenaltySource.Other)]
    public void Create_WithDifferentSources_ShouldSetCorrectly(PenaltySource source)
    {
        var record = PenaltyRecord.Create(
            _tenantId, source,
            "Test penalty", 100m, DateTime.UtcNow);

        record.Source.Should().Be(source);
    }

    [Fact]
    public void Create_WithDueDate_ShouldSet()
    {
        var dueDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.TaxAuthority,
            "Gecikme faizi", 500m, DateTime.UtcNow,
            dueDate: dueDate);

        record.DueDate.Should().Be(dueDate);
    }

    [Fact]
    public void Create_WithReferenceNumber_ShouldSet()
    {
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.SGK,
            "SGK cezasi", 1000m, DateTime.UtcNow,
            referenceNumber: "SGK-2026-001");

        record.ReferenceNumber.Should().Be("SGK-2026-001");
    }

    [Fact]
    public void Create_WithRelatedOrderId_ShouldSet()
    {
        var orderId = Guid.NewGuid();
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.Trendyol,
            "Gec kargo", 150m, DateTime.UtcNow,
            relatedOrderId: orderId);

        record.RelatedOrderId.Should().Be(orderId);
    }

    [Fact]
    public void Create_WithCustomCurrency_ShouldSet()
    {
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.Amazon,
            "Late shipment", 25m, DateTime.UtcNow,
            currency: "EUR");

        record.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Create_WithNotes_ShouldSet()
    {
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.Trendyol,
            "Gec kargo", 150m, DateTime.UtcNow,
            notes: "Siparis gecikme nedeniyle");

        record.Notes.Should().Be("Siparis gecikme nedeniyle");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var r1 = PenaltyRecord.Create(_tenantId, PenaltySource.Trendyol,
            "Penalty 1", 100m, DateTime.UtcNow);
        var r2 = PenaltyRecord.Create(_tenantId, PenaltySource.Trendyol,
            "Penalty 2", 200m, DateTime.UtcNow);

        r1.Id.Should().NotBe(r2.Id);
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var record = PenaltyRecord.Create(
            _tenantId, PenaltySource.Trendyol,
            "Test", 100m, DateTime.UtcNow);

        record.TenantId.Should().Be(_tenantId);
    }
}
