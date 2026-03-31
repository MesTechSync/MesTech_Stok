using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Hr.Commands.ApproveLeave;

public sealed class ApproveLeaveHandler : IRequestHandler<ApproveLeaveCommand, Unit>
{
    private readonly ILeaveRepository _leaves;
    private readonly IUnitOfWork _uow;

    public ApproveLeaveHandler(ILeaveRepository leaves, IUnitOfWork uow)
        => (_leaves, _uow) = (leaves, uow);

    public async Task<Unit> Handle(ApproveLeaveCommand request, CancellationToken cancellationToken)
    {
        var leave = await _leaves.GetByIdAsync(request.LeaveId, cancellationToken)
            ?? throw new InvalidOperationException($"Leave {request.LeaveId} not found.");

        leave.Approve(request.ApproverUserId);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
