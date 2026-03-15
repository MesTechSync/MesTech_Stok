using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Queries.GetSyncStatus;

public class GetSyncStatusHandler : IRequestHandler<GetSyncStatusQuery, SyncStatusResult>
{
    private readonly IIntegratorOrchestrator _orchestrator;

    public GetSyncStatusHandler(IIntegratorOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<SyncStatusResult> Handle(GetSyncStatusQuery request, CancellationToken cancellationToken)
    {
        var result = new SyncStatusResult();

        foreach (var adapter in _orchestrator.RegisteredAdapters)
        {
            if (request.PlatformCode != null && !string.Equals(adapter.PlatformCode, request.PlatformCode, StringComparison.Ordinal))
            {
                continue;
            }

            result.Platforms.Add(new PlatformSyncStatus
            {
                PlatformCode = adapter.PlatformCode,
                PlatformName = adapter.PlatformCode,
                IsEnabled = true,
                IsConnected = true,
            });
        }

        return Task.FromResult(result);
    }
}
