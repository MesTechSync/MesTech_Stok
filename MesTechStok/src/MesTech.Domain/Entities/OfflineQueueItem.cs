using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class OfflineQueueItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Channel { get; set; } = "Generic";
    public string Direction { get; set; } = "Out";
    public string? Payload { get; set; }
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public string? CorrelationId { get; set; }
}
