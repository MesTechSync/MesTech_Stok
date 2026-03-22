namespace MesTech.Domain.Entities.Billing;

/// <summary>Abonelik durumu.</summary>
public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    PastDue = 2,
    Cancelled = 3,
    Expired = 4
}
