namespace MesTech.Domain.Enums;

/// <summary>
/// Platform ödeme durumları.
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Scheduled = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    PartiallyPaid = 5,
    Cancelled = 6
}
