using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Hr;

public class WorkSchedule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public bool IsWorkDay { get; private set; }
    public string? Notes { get; private set; }

    private WorkSchedule() { }

    public static WorkSchedule Create(
        Guid tenantId, Guid employeeId, DayOfWeek dayOfWeek,
        TimeSpan startTime, TimeSpan endTime, bool isWorkDay = true, string? notes = null)
    {
        return new WorkSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsWorkDay = isWorkDay,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
