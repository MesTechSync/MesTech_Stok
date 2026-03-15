using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Hr.Commands.ApproveLeave;

public class ApproveLeaveHandler : IRequestHandler<ApproveLeaveCommand, Unit>
{
    private readonly ILeaveRepository _leaves;
    private readonly IUnitOfWork _uow;

    public ApproveLeaveHandler(ILeaveRepository leaves, IUnitOfWork uow)
        => (_leaves, _uow) = (leaves, uow);

    public async Task<Unit> Handle(ApproveLeaveCommand req, CancellationToken ct)
    {
        var leave = await _leaves.GetByIdAsync(req.LeaveId, ct)
            ?? throw new InvalidOperationException($"Leave {req.LeaveId} not found.");

        leave.Approve(req.ApproverUserId);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
