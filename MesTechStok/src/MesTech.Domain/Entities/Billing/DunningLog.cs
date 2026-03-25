using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Billing;

/// <summary>
/// Odeme tahsilat kaydi — basarisiz odeme sonrasi uyari/yeniden deneme/askiya alma/iptal adimlari.
/// </summary>
public sealed class DunningLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TenantSubscriptionId { get; private set; }
    public int AttemptNumber { get; private set; }
    public DateTime AttemptDate { get; private set; }
    public DunningAction Action { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Navigation
    public TenantSubscription? Subscription { get; private set; }

    private DunningLog() { }

    public static DunningLog Create(
        Guid tenantId,
        Guid tenantSubscriptionId,
        int attemptNumber,
        DunningAction action,
        bool success,
        string? errorMessage = null)
    {
        var now = DateTime.UtcNow;
        return new DunningLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantSubscriptionId = tenantSubscriptionId,
            AttemptNumber = attemptNumber,
            AttemptDate = now,
            Action = action,
            Success = success,
            ErrorMessage = errorMessage,
            CreatedAt = now
        };
    }
}
