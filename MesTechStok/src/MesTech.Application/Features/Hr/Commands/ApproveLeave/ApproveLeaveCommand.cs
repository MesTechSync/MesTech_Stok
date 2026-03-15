using MediatR;

namespace MesTech.Application.Features.Hr.Commands.ApproveLeave;

public record ApproveLeaveCommand(Guid LeaveId, Guid ApproverUserId) : IRequest<Unit>;
