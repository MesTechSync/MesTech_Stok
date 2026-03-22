using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface ILeaveRejectedEventHandler
{
    Task HandleAsync(Guid leaveId, Guid employeeId, string reason, CancellationToken ct);
}

public class LeaveRejectedEventHandler : ILeaveRejectedEventHandler
{
    private readonly ILogger<LeaveRejectedEventHandler> _logger;

    public LeaveRejectedEventHandler(ILogger<LeaveRejectedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(Guid leaveId, Guid employeeId, string reason, CancellationToken ct)
    {
        _logger.LogInformation(
            "İzin talebi reddedildi — LeaveId={LeaveId}, EmployeeId={EmployeeId}, Reason={Reason}",
            leaveId, employeeId, reason);

        return Task.CompletedTask;
    }
}
