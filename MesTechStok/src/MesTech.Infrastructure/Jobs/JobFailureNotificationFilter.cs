using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using MesTech.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Hangfire global filter — job FailedState'e gectiginde admin notification gonderir.
/// Prometheus metrik kaydeder. DEV3 TUR6: Job failure silent-fail onleme.
/// GlobalJobFilters.Filters.Add() ile aktif edilir.
/// </summary>
public sealed class JobFailureNotificationFilter : JobFilterAttribute, IElectStateFilter
{
    public ILogger<JobFailureNotificationFilter> Logger { get; }

    public JobFailureNotificationFilter(ILogger<JobFailureNotificationFilter> logger)
    {
        Logger = logger;
    }

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState failedState)
            return;

        var jobId = context.BackgroundJob.Id;
        var jobType = context.BackgroundJob.Job?.Type?.Name ?? "Unknown";
        var methodName = context.BackgroundJob.Job?.Method?.Name ?? "Unknown";
        var errorMessage = failedState.Exception?.Message ?? "No exception details";
        var exceptionType = failedState.Exception?.GetType().Name ?? "Unknown";

        Logger.LogError(
            "[Hangfire FAIL] JobId={JobId} Type={JobType}.{Method} Error={ExceptionType}: {Error}",
            jobId, jobType, methodName, exceptionType, errorMessage);

        // Prometheus metrik
        try
        {
            AdapterMetrics.ApiCallsTotal
                .WithLabels("hangfire", $"job.{jobType.ToLowerInvariant()}", "failed")
                .Inc();
        }
        catch
        {
            // Prometheus unavailable — don't block state transition
        }
    }
}
