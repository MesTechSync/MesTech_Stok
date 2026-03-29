using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Hr.Queries.GetLeaveRequests;

public sealed class GetLeaveRequestsHandler
    : IRequestHandler<GetLeaveRequestsQuery, IReadOnlyList<LeaveRequestDto>>
{
    private readonly ILeaveRepository _leaveRepo;

    public GetLeaveRequestsHandler(ILeaveRepository leaveRepo)
        => _leaveRepo = leaveRepo;

    public async Task<IReadOnlyList<LeaveRequestDto>> Handle(
        GetLeaveRequestsQuery request, CancellationToken ct)
    {
        var leaves = await _leaveRepo.GetByTenantAsync(request.TenantId, request.Status, ct);

        return leaves.Select(l => new LeaveRequestDto
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee?.EmployeeCode ?? l.EmployeeId.ToString("N")[..8],
            LeaveType = l.Type.ToString(),
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            TotalDays = l.TotalDays,
            Status = l.Status.ToString(),
            Reason = l.Reason,
            CreatedAt = l.CreatedAt
        }).ToList();
    }
}
