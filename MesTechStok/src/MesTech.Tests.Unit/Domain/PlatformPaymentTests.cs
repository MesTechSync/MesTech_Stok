using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Feature", "PlatformPayment")]
[Trait("Phase", "Dalga5")]
public class PlatformPaymentTests
{
    private static PlatformPayment MakePayment(
        PaymentStatus status = PaymentStatus.Pending,
        DateTime? scheduledDate = null)
    {
        var payment = new PlatformPayment
        {
            TenantId = Guid.NewGuid(),
            Platform = PlatformType.Trendyol,
            PeriodStart = DateTime.UtcNow.AddDays(-7),
            PeriodEnd = DateTime.UtcNow,
            ScheduledPaymentDate = scheduledDate,
            Currency = "TRY"
        };
        payment.SetAmounts(1000m, 100m, 20m, 50m, 10m);
        if (status == PaymentStatus.Completed)
            payment.MarkAsCompleted();
        else if (status == PaymentStatus.Failed)
            payment.MarkAsFailed();
        return payment;
    }

    [Fact]
    public void CalculateNetAmount_SubtractsAllDeductions_Correctly()
    {
        var payment = MakePayment();
        payment.CalculateNetAmount();
        // 1000 - 100 - 20 - 50 - 10 = 820
        payment.NetAmount.Should().Be(820m);
    }

    [Fact]
    public void CalculateNetAmount_ZeroDeductions_EqualsGrossSales()
    {
        var payment = new PlatformPayment
        {
            TenantId = Guid.NewGuid(),
        };
        payment.SetAmounts(500m, 0m, 0m, 0m, 0m);
        payment.NetAmount.Should().Be(500m);
    }

    [Fact]
    public void MarkAsCompleted_SetsStatusAndDate()
    {
        var payment = MakePayment();
        payment.MarkAsCompleted("TRX-123");

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.ActualPaymentDate.Should().NotBeNull();
        payment.BankReference.Should().Be("TRX-123");
    }

    [Fact]
    public void MarkAsCompleted_WithoutReference_SetsNullReference()
    {
        var payment = MakePayment();
        payment.MarkAsCompleted();

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.BankReference.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_SetsStatusAndReason()
    {
        var payment = MakePayment();
        payment.MarkAsFailed("Banka reddi");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.Notes.Should().Be("Banka reddi");
    }

    [Fact]
    public void IsOverdue_PendingAndPastScheduledDate_ReturnsTrue()
    {
        var payment = MakePayment(
            status: PaymentStatus.Pending,
            scheduledDate: DateTime.UtcNow.AddDays(-1));
        payment.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_PendingAndFutureScheduledDate_ReturnsFalse()
    {
        var payment = MakePayment(
            status: PaymentStatus.Pending,
            scheduledDate: DateTime.UtcNow.AddDays(3));
        payment.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_CompletedPayment_ReturnsFalse()
    {
        var payment = MakePayment(
            status: PaymentStatus.Completed,
            scheduledDate: DateTime.UtcNow.AddDays(-5));
        payment.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_NoScheduledDate_ReturnsFalse()
    {
        var payment = MakePayment(status: PaymentStatus.Pending, scheduledDate: null);
        payment.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void DefaultStatus_IsPending()
    {
        var payment = new PlatformPayment();
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Currency.Should().Be("TRY");
    }
}
