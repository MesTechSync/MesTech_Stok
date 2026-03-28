using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Commands.TriggerSync;

public sealed class TriggerSyncHandler : IRequestHandler<TriggerSyncCommand, TriggerSyncResult>
{
    private readonly IBackgroundJobService _jobService;
    private readonly ILogger<TriggerSyncHandler> _logger;

    public TriggerSyncHandler(IBackgroundJobService jobService, ILogger<TriggerSyncHandler> logger)
    {
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
            var jobId = _jobService.Enqueue(() =>
                Task.Run(() => _logger.LogInformation("Sync job executing for {Platform}", platform)));

            return Task.FromResult(new TriggerSyncResult
            {
                IsSuccess = true,
                JobId = jobId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger sync for {Platform}", request.PlatformCode);
            return Task.FromResult(new TriggerSyncResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
        }
    }
}
