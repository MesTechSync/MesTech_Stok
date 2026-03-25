using MesTech.Domain.Events.Tasks;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Görev tamamlandığında loglama yapar.
/// </summary>
public interface ITaskCompletedEventHandler
{
    Task HandleAsync(TaskCompletedEvent domainEvent, CancellationToken ct);
}

public sealed class TaskCompletedEventHandler : ITaskCompletedEventHandler
{
    private readonly ILogger<TaskCompletedEventHandler> _logger;

    public TaskCompletedEventHandler(ILogger<TaskCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(TaskCompletedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "TaskCompleted — TaskId={TaskId}, CompletedByUserId={CompletedByUserId}, OccurredAt={OccurredAt}",
            domainEvent.TaskId, domainEvent.CompletedByUserId, domainEvent.OccurredAt);

        return Task.CompletedTask;
    }
}
