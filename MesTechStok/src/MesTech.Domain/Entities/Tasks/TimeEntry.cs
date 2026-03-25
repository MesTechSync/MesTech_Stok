using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Tasks;

public sealed class TimeEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid WorkTaskId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public int Minutes { get; private set; }
    public string? Description { get; private set; }
    public bool IsBillable { get; private set; }
    public decimal? HourlyRate { get; private set; }

    private TimeEntry() { }

    public static TimeEntry Start(Guid tenantId, Guid workTaskId, Guid userId, string? description = null, bool isBillable = false, decimal? hourlyRate = null)
    {
        return new TimeEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkTaskId = workTaskId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            Description = description,
            IsBillable = isBillable,
            HourlyRate = hourlyRate,
            Minutes = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Stop()
    {
        EndedAt = DateTime.UtcNow;
        Minutes = (int)(EndedAt.Value - StartedAt).TotalMinutes;
        UpdatedAt = DateTime.UtcNow;
    }
}
