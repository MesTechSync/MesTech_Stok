using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Commands.TriggerSync;

public sealed class TriggerSyncHandler : IRequestHandler<TriggerSyncCommand, TriggerSyncResult>
{
    private readonly IIntegratorOrchestrator _orchestrator;
    private readonly IBackgroundJobService _jobService;
    private readonly ILogger<TriggerSyncHandler> _logger;

    public TriggerSyncHandler(
        IIntegratorOrchestrator orchestrator,
        IBackgroundJobService jobService,
        ILogger<TriggerSyncHandler> logger)
    {
        _orchestrator = orchestrator;
        _jobService = jobService;
        _logger = logger;
    }

    public Task<TriggerSyncResult> Handle(TriggerSyncCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual sync triggered for {Platform} by tenant {TenantId}",
            request.PlatformCode, request.TenantId);

        try
        {
            var platform = request.PlatformCode;

            // Gerçek sync — IIntegratorOrchestrator.SyncPlatformAsync delegate
            // Hangfire background job olarak enqueue et (UI bloklamadan)
            var jobId = _jobService.Enqueue(() =>
                _orchestrator.SyncPlatformAsync(platform, CancellationToken.None));

            return Task.FromResult(new TriggerSyncResult
            {
                IsSuccess = true,
                JobId = jobId
            });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger sync for {Platform}", request.PlatformCode);
            return Task.FromResult(new TriggerSyncResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
        }
#pragma warning restore CA1031
    }
}
