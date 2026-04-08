using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Hr;

namespace MesTech.Domain.Entities.Hr;

public sealed class Leave : BaseEntity, ITenantEntity
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
    public string? RejectionReason { get; private set; }
    public Employee Employee { get; private set; } = null!;

    private Leave() { }

    public static Leave Create(Guid tenantId, Guid employeeId, LeaveType type,
        DateTime startDate, DateTime endDate, string? reason = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date.", nameof(endDate));
        int workDays = 0;
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                workDays++;
        int total = (int)(endDate - startDate).TotalDays + 1;
        return new Leave
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EmployeeId = employeeId,
            Type = type, StartDate = startDate.Date, EndDate = endDate.Date,
            TotalDays = workDays > 0 ? workDays : total,
            Status = LeaveStatus.Pending, Reason = reason, CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(Guid approverUserId)
    {
        if (Status != LeaveStatus.Pending)
            throw new InvalidOperationException("Only pending leaves can be approved.");
        Status = LeaveStatus.Approved; ApprovedByUserId = approverUserId;
        ApprovedAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new LeaveApprovedEvent(Id, TenantId, EmployeeId, DateTime.UtcNow));
    }

    public void Reject(Guid approverUserId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != LeaveStatus.Pending)
            throw new InvalidOperationException("Only pending leaves can be rejected.");
        Status = LeaveStatus.Rejected; ApprovedByUserId = approverUserId;
        RejectionReason = reason; UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new LeaveRejectedEvent(Id, TenantId, EmployeeId, reason, DateTime.UtcNow));
    }

    public void Cancel()
    {
        if (Status == LeaveStatus.Approved && StartDate <= DateTime.UtcNow.Date)
            throw new InvalidOperationException("Cannot cancel a leave that has already started.");
        Status = LeaveStatus.Cancelled; UpdatedAt = DateTime.UtcNow;
    }
}
