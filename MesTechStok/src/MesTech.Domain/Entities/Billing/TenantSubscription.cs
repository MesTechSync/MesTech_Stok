using MesTech.Domain.Common;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities.Billing;

/// <summary>
/// Tenant'in aktif aboneligi — hangi plan, ne zaman basladi, odeme durumu.
/// </summary>
public class TenantSubscription : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public BillingPeriod Period { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? NextBillingDate { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    // Navigation
    public SubscriptionPlan? Plan { get; private set; }

    private TenantSubscription() { }

    public static TenantSubscription StartTrial(Guid tenantId, Guid planId, int trialDays = 14)
    {
        var now = DateTime.UtcNow;
        return new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = planId,
            Status = SubscriptionStatus.Trial,
            Period = BillingPeriod.Monthly,
            StartDate = now,
            TrialEndsAt = now.AddDays(trialDays),
            NextBillingDate = now.AddDays(trialDays),
            CreatedAt = now
        };
    }

    public static TenantSubscription Activate(Guid tenantId, Guid planId, BillingPeriod period)
    {
        var now = DateTime.UtcNow;
        var nextBilling = period == BillingPeriod.Annual
            ? now.AddYears(1)
            : now.AddMonths(1);

        var sub = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            Period = period,
            StartDate = now,
            NextBillingDate = nextBilling,
            CreatedAt = now
        };
        sub.RaiseDomainEvent(new SubscriptionCreatedEvent(tenantId, sub.Id, planId, SubscriptionStatus.Active, now));
        return sub;
    }

    public void Renew()
    {
        Status = SubscriptionStatus.Active;
        NextBillingDate = Period == BillingPeriod.Annual
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPastDue()
    {
        Status = SubscriptionStatus.PastDue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        EndDate = NextBillingDate; // Donem sonuna kadar aktif kalir
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new SubscriptionCancelledEvent(TenantId, Id, reason, DateTime.UtcNow));
    }

    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
        EndDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConvertFromTrial(BillingPeriod period)
    {
        if (Status != SubscriptionStatus.Trial)
            throw new InvalidOperationException("Sadece trial abonelik donusturulebilir.");

        Status = SubscriptionStatus.Active;
        Period = period;
        NextBillingDate = period == BillingPeriod.Annual
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
        TrialEndsAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired => Status == SubscriptionStatus.Expired ||
        (TrialEndsAt.HasValue && TrialEndsAt.Value < DateTime.UtcNow && Status == SubscriptionStatus.Trial);
}

/// <summary>Abonelik durumu.</summary>
public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    PastDue = 2,
    Cancelled = 3,
    Expired = 4
}

/// <summary>Faturalama donemi.</summary>
public enum BillingPeriod
{
    Monthly = 0,
    Annual = 1
}
