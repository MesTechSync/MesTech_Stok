using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Tasks;

public class Milestone : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime? DueDate { get; private set; }
    public MilestoneStatus Status { get; private set; }
    public int Position { get; private set; }

    private Milestone() { }

    public static Milestone Create(Guid tenantId, Guid projectId, string name, int position, DateTime? dueDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Milestone
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            Name = name,
            Position = position,
            DueDate = dueDate,
            Status = MilestoneStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkDone() { Status = MilestoneStatus.Done; UpdatedAt = DateTime.UtcNow; }
    public void MarkOverdue() { Status = MilestoneStatus.Overdue; UpdatedAt = DateTime.UtcNow; }
}
