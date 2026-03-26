using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Commands.SyncHepsiburadaProducts;

public sealed class SyncHepsiburadaProductsHandler : IRequestHandler<SyncHepsiburadaProductsCommand, SyncResultDto>
{
    private readonly IIntegratorOrchestrator _orchestrator;

    public SyncHepsiburadaProductsHandler(IIntegratorOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task<SyncResultDto> Handle(SyncHepsiburadaProductsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _orchestrator.SyncPlatformAsync("HEPSIBURADA", cancellationToken).ConfigureAwait(false);
    }
}
