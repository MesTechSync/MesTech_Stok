using MesTech.Domain.Events.Tasks;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Görev süresi geçtiğinde uyarı loglama yapar.
/// </summary>
public interface ITaskOverdueEventHandler
{
    Task HandleAsync(TaskOverdueEvent domainEvent, CancellationToken ct);
}

public class TaskOverdueEventHandler : ITaskOverdueEventHandler
{
    private readonly ILogger<TaskOverdueEventHandler> _logger;

    public TaskOverdueEventHandler(ILogger<TaskOverdueEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(TaskOverdueEvent domainEvent, CancellationToken ct)
    {
        _logger.LogWarning(
            "TaskOverdue — TaskId={TaskId}, DueDate={DueDate}, OccurredAt={OccurredAt}",
            domainEvent.TaskId, domainEvent.DueDate, domainEvent.OccurredAt);

        return Task.CompletedTask;
    }
}
