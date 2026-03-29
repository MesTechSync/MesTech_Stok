using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Hr.Queries.GetLeaveRequests;

/// <summary>
/// İzin talepleri listesi — durum filtreli.
/// G207-DEV6: HrEndpoints /leaves endpoint eksikliği kapatılıyor.
/// </summary>
public record GetLeaveRequestsQuery(
    Guid TenantId,
    LeaveStatus? Status = null
) : IRequest<IReadOnlyList<LeaveRequestDto>>;

public sealed class LeaveRequestDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string LeaveType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalDays { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public DateTime CreatedAt { get; init; }
}
