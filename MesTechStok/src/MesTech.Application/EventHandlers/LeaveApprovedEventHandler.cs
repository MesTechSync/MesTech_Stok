using MesTech.Domain.Events.Hr;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface ILeaveApprovedEventHandler
{
    Task HandleAsync(LeaveApprovedEvent domainEvent, CancellationToken ct);
}

public class LeaveApprovedEventHandler : ILeaveApprovedEventHandler
{
    private readonly ILogger<LeaveApprovedEventHandler> _logger;

    public LeaveApprovedEventHandler(ILogger<LeaveApprovedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(LeaveApprovedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "LeaveApproved: LeaveId={LeaveId}, EmployeeId={EmployeeId}",
            domainEvent.LeaveId, domainEvent.EmployeeId);

        return Task.CompletedTask;
    }
}
