using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class SyncRetryItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string SyncType { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string? ItemData { get; set; }
    public string LastError { get; private set; } = string.Empty;
    public string ErrorCategory { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }
    public int MaxRetries { get; set; } = 3;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastRetryUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? NextRetryUtc { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTime? ResolvedUtc { get; private set; }
    public string? CorrelationId { get; set; }
    public string? AdditionalInfo { get; set; }

    public void CalculateNextRetry()
    {
        var backoffSeconds = Math.Pow(2, RetryCount) * 60;
        NextRetryUtc = DateTime.UtcNow.AddSeconds(Math.Min(backoffSeconds, 86400));
    }

    public void IncrementRetry(string error, string category)
    {
        RetryCount++;
        LastError = error;
        ErrorCategory = category;
        LastRetryUtc = DateTime.UtcNow;
        CalculateNextRetry();
    }

    public void MarkAsResolved()
    {
        IsResolved = true;
        ResolvedUtc = DateTime.UtcNow;
        NextRetryUtc = null;
    }
}
