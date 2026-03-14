using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Crm;

public class Activity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public ActivityType Type { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public ActivityDirection? Direction { get; private set; }
    public int? DurationMinutes { get; private set; }
    public Guid? CrmContactId { get; private set; }
    public Guid? DealId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? LeadId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public bool IsCompleted { get; private set; }

    private Activity() { }

    public static Activity Create(
        Guid tenantId, ActivityType type, string subject,
        Guid createdByUserId, DateTime? occurredAt = null,
        string? description = null, ActivityDirection? direction = null,
        int? durationMinutes = null, Guid? crmContactId = null,
        Guid? dealId = null, Guid? orderId = null, Guid? leadId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        return new Activity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            Subject = subject,
            Description = description,
            OccurredAt = occurredAt ?? DateTime.UtcNow,
            Direction = direction,
            DurationMinutes = durationMinutes,
            CrmContactId = crmContactId,
            DealId = dealId,
            OrderId = orderId,
            LeadId = leadId,
            CreatedByUserId = createdByUserId,
            IsCompleted = type != ActivityType.Meeting,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete()
    {
        IsCompleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
