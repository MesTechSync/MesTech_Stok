using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Tasks;

namespace MesTech.Domain.Entities.Tasks;

public sealed class WorkTask : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? ProjectId { get; private set; }
    public Guid? MilestoneId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; }
    public WorkTaskStatus Status { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public int? EstimatedMinutes { get; private set; }
    public int? ActualMinutes { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? CrmContactId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string? Tags { get; private set; }
    public int Position { get; private set; }

    private WorkTask() { }

    public static WorkTask Create(
        Guid tenantId, string title, TaskPriority priority = TaskPriority.Normal,
        Guid? projectId = null, Guid? milestoneId = null,
        Guid? assignedToUserId = null, Guid? createdByUserId = null,
        DateTime? dueDate = null, int? estimatedMinutes = null,
        Guid? orderId = null, Guid? crmContactId = null, Guid? productId = null,
        int position = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return new WorkTask
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Priority = priority,
            Status = WorkTaskStatus.Backlog,
            ProjectId = projectId,
            MilestoneId = milestoneId,
            AssignedToUserId = assignedToUserId,
            CreatedByUserId = createdByUserId,
            DueDate = dueDate,
            EstimatedMinutes = estimatedMinutes,
            OrderId = orderId,
            CrmContactId = crmContactId,
            ProductId = productId,
            Position = position,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void StartWork(Guid userId)
    {
        Status = WorkTaskStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        AssignedToUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(Guid userId)
    {
        Status = WorkTaskStatus.Done;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new TaskCompletedEvent(Id, TenantId, userId, DateTime.UtcNow));
    }

    public void AssignTo(Guid userId)
    {
        AssignedToUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToStatus(WorkTaskStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gecikme kontrolu — DueDate gecmisse ve tamamlanmamissa TaskOverdueEvent firlatir.
    /// Hangfire job'dan periyodik olarak cagrilir.
    /// </summary>
    public bool CheckOverdue()
    {
        if (DueDate.HasValue && DueDate.Value < DateTime.UtcNow
            && Status is not (WorkTaskStatus.Done or WorkTaskStatus.Cancelled))
        {
            RaiseDomainEvent(new TaskOverdueEvent(Id, TenantId, DueDate.Value, DateTime.UtcNow));
            return true;
        }
        return false;
    }
}
