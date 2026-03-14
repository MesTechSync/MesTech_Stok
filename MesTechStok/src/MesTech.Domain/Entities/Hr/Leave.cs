using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Hr;

namespace MesTech.Domain.Entities.Hr;

public class Leave : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; private set; }
    public LeaveType Type { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int TotalDays { get; private set; }
    public LeaveStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    private Leave() { }

    public static Leave Create(
        Guid tenantId, Guid employeeId, LeaveType type,
        DateTime startDate, DateTime endDate, string? reason = null)
    {
        if (endDate < startDate) throw new ArgumentException("EndDate must be >= StartDate.");
        return new Leave
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = (int)(endDate - startDate).TotalDays + 1,
            Status = LeaveStatus.Pending,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(Guid approverUserId)
    {
        if (Status != LeaveStatus.Pending) throw new InvalidOperationException("Only pending leaves can be approved.");
        Status = LeaveStatus.Approved;
        ApprovedByUserId = approverUserId;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new LeaveApprovedEvent(Id, EmployeeId, DateTime.UtcNow));
    }

    public void Reject(Guid approverUserId, string reason)
    {
        if (Status != LeaveStatus.Pending) throw new InvalidOperationException("Only pending leaves can be rejected.");
        Status = LeaveStatus.Rejected;
        ApprovedByUserId = approverUserId;
        Reason = reason;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new LeaveRejectedEvent(Id, EmployeeId, reason, DateTime.UtcNow));
    }
}
