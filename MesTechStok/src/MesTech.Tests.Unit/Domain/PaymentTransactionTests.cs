using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// PaymentTransaction entity domain logic tests.
/// Bu testler kirilirsa = odeme islemi mantigi bozulmus demektir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "PaymentTransaction")]
public class PaymentTransactionTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private static readonly Guid ValidOrderId = Guid.NewGuid();

    private static PaymentTransaction CreatePendingTransaction(
        decimal amount = 150.00m,
        int installmentCount = 1)
        => PaymentTransaction.Create(
            tenantId: ValidTenantId,
            orderId: ValidOrderId,
            provider: PaymentProviderType.PayTRDirect,
            amount: amount,
            currency: "TRY",
            installmentCount: installmentCount);

    // ════ 1. Create_ValidParams_Success ════

    [Fact]
    public void Create_ValidParams_Success()
    {
        // Act
        var tx = PaymentTransaction.Create(
            tenantId: ValidTenantId,
            orderId: ValidOrderId,
            provider: PaymentProviderType.PayTRDirect,
            amount: 299.99m,
            currency: "TRY",
            installmentCount: 3);

        // Assert
        tx.TenantId.Should().Be(ValidTenantId);
        tx.OrderId.Should().Be(ValidOrderId);
        tx.Provider.Should().Be(PaymentProviderType.PayTRDirect);
        tx.Amount.Should().Be(299.99m);
        tx.Currency.Should().Be("TRY");
        tx.InstallmentCount.Should().Be(3);
        tx.Status.Should().Be(PaymentTransactionStatus.Pending);
        tx.PaidAt.Should().BeNull();
        tx.RefundedAt.Should().BeNull();
        tx.Id.Should().NotBe(Guid.Empty);
    }

    // ════ 2. Create_NegativeAmount_Throws ════

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        // Act
        var act = () => PaymentTransaction.Create(
            tenantId: ValidTenantId,
            orderId: ValidOrderId,
            provider: PaymentProviderType.PayTRiFrame,
            amount: -50.00m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("amount");
    }

    // ════ 3. Create_EmptyOrderId_Throws ════

    [Fact]
    public void Create_EmptyOrderId_Throws()
    {
        // Act
        var act = () => PaymentTransaction.Create(
            tenantId: ValidTenantId,
            orderId: Guid.Empty,
            provider: PaymentProviderType.PayTRDirect,
            amount: 100.00m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("orderId");
    }

    // ════ 4. MarkCompleted_FromPending_Success ════

    [Fact]
    public void MarkCompleted_FromPending_Success()
    {
        // Arrange
        var tx = CreatePendingTransaction();
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        tx.MarkCompleted("PAYTR-TX-12345");

        // Assert
        tx.Status.Should().Be(PaymentTransactionStatus.Completed);
        tx.TransactionId.Should().Be("PAYTR-TX-12345");
        tx.PaidAt.Should().NotBeNull();
        tx.PaidAt!.Value.Should().BeAfter(before);
    }

    // ════ 5. MarkCompleted_FromCompleted_Throws ════

    [Fact]
    public void MarkCompleted_FromCompleted_Throws()
    {
        // Arrange
        var tx = CreatePendingTransaction();
        tx.MarkCompleted("PAYTR-TX-FIRST");

        // Act — attempt to complete an already-completed transaction
        var act = () => tx.MarkCompleted("PAYTR-TX-SECOND");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ════ 6. MarkFailed_FromPending_Success ════

    [Fact]
    public void MarkFailed_FromPending_Success()
    {
        // Arrange
        var tx = CreatePendingTransaction();

        // Act
        tx.MarkFailed();

        // Assert
        tx.Status.Should().Be(PaymentTransactionStatus.Failed);
        tx.PaidAt.Should().BeNull("basarisiz islemde odeme zamani set edilmemeli");
    }

    // ════ 7. MarkFailed_FromCompleted_Throws ════

    [Fact]
    public void MarkFailed_FromCompleted_Throws()
    {
        // Arrange
        var tx = CreatePendingTransaction();
        tx.MarkCompleted("PAYTR-TX-DONE");

        // Act — cannot fail a completed payment
        var act = () => tx.MarkFailed();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ════ 8. MarkRefunded_FromCompleted_Success ════

    [Fact]
    public void MarkRefunded_FromCompleted_Success()
    {
        // Arrange
        var tx = CreatePendingTransaction();
        tx.MarkCompleted("PAYTR-TX-REFUND");
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        tx.MarkRefunded();

        // Assert
        tx.Status.Should().Be(PaymentTransactionStatus.Refunded);
        tx.RefundedAt.Should().NotBeNull();
        tx.RefundedAt!.Value.Should().BeAfter(before);
    }

    // ════ 9. MarkRefunded_FromPending_Throws ════

    [Fact]
    public void MarkRefunded_FromPending_Throws()
    {
        // Arrange — transaction is still Pending, cannot refund
        var tx = CreatePendingTransaction();

        // Act
        var act = () => tx.MarkRefunded();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Completed*");
    }

    // ════ 10. InstallmentCount_DefaultIsOne ════

    [Fact]
    public void InstallmentCount_DefaultIsOne()
    {
        // Act — create without specifying installment count
        var tx = PaymentTransaction.Create(
            tenantId: ValidTenantId,
            orderId: ValidOrderId,
            provider: PaymentProviderType.PayTRDirect,
            amount: 100.00m);

        // Assert
        tx.InstallmentCount.Should().Be(1);
    }
}
