using System.Linq.Expressions;
using Hangfire;
using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Hangfire-based IBackgroundJobService implementation.
/// Wraps Hangfire static API behind testable interface.
/// </summary>
public sealed class HangfireBackgroundJobService : IBackgroundJobService
{
    public string Enqueue(Expression<Func<Task>> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
        => BackgroundJob.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring(string jobId, Expression<Func<Task>> methodCall, string cronExpression)
        => RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);

    public void RemoveRecurring(string jobId)
        => RecurringJob.RemoveIfExists(jobId);
}
