using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Commands.SyncN11Products;

public sealed class SyncN11ProductsHandler : IRequestHandler<SyncN11ProductsCommand, SyncResultDto>
{
    private readonly IIntegratorOrchestrator _orchestrator;

    public SyncN11ProductsHandler(IIntegratorOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task<SyncResultDto> Handle(SyncN11ProductsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _orchestrator.SyncPlatformAsync("N11", cancellationToken).ConfigureAwait(false);
    }
}
