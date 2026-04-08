using System.Diagnostics;
using Hangfire.Server;
using MesTech.Infrastructure.Monitoring;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Hangfire global server filter — tüm 43 job için execution metriği kaydeder.
/// OnPerforming: in-flight gauge artır, stopwatch başlat.
/// OnPerformed: başarı/hata say, süre histogram'a yaz, in-flight gauge azalt.
///
/// Kayıt: HangfireConfig.AddGlobalFilter(new JobExecutionMetricsFilter())
/// Yararı: 43 job dosyasına tek tek metrik eklemek gerekmez.
/// </summary>
public sealed class JobExecutionMetricsFilter : IServerFilter
{
    private const string StopwatchKey = "metrics.stopwatch";
    private const string JobTypeKey = "metrics.jobtype";

    public void OnPerforming(PerformingContext context)
    {
        var jobType = context.BackgroundJob.Job?.Type?.Name ?? "Unknown";
        context.Items[JobTypeKey] = jobType;
        context.Items[StopwatchKey] = Stopwatch.StartNew();

        try
        {
            JobMetrics.JobsInFlight.WithLabels(jobType).Inc();
        }
        catch
        {
            // Prometheus unavailable — non-fatal
        }
    }

    public void OnPerformed(PerformedContext context)
    {
        var jobType = context.Items.TryGetValue(JobTypeKey, out var jt) ? jt as string ?? "Unknown" : "Unknown";
        var sw = context.Items.TryGetValue(StopwatchKey, out var swObj) ? swObj as Stopwatch : null;
        sw?.Stop();

        var status = context.Exception == null ? "success" : "exception";

        try
        {
            JobMetrics.JobExecutionsTotal.WithLabels(jobType, status).Inc();

            if (sw is not null)
                JobMetrics.JobDurationSeconds.WithLabels(jobType).Observe(sw.Elapsed.TotalSeconds);

            JobMetrics.JobsInFlight.WithLabels(jobType).Dec();
        }
        catch
        {
            // Prometheus unavailable — non-fatal
        }
    }
}
