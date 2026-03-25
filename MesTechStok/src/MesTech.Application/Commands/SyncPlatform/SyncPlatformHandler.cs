using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
namespace MesTech.Application.Commands.SyncPlatform;

public sealed class SyncPlatformHandler : IRequestHandler<SyncPlatformCommand, SyncResultDto>
{
    private readonly IIntegratorOrchestrator _orchestrator;

    public SyncPlatformHandler(IIntegratorOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task<SyncResultDto> Handle(SyncPlatformCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.Equals(request.PlatformCode, "*", StringComparison.Ordinal))
            return await _orchestrator.SyncAllPlatformsAsync(cancellationToken).ConfigureAwait(false);

        return await _orchestrator.SyncPlatformAsync(request.PlatformCode, cancellationToken).ConfigureAwait(false);
    }
}
