namespace MesTech.Domain.Enums;

/// <summary>
/// İade talebi durumları.
/// </summary>
public enum ReturnStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    InTransit = 3,
    Received = 4,
    Refunded = 5,
    Cancelled = 6
}
