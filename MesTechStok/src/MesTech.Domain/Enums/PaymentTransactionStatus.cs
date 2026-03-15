namespace MesTech.Domain.Enums;

/// <summary>
/// Odeme islemi durumu — PaymentTransaction entity lifecycle.
/// </summary>
public enum PaymentTransactionStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4,
    PartialRefund = 5
}
