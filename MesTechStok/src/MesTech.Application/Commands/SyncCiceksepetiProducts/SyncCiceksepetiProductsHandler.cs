using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Commands.SyncCiceksepetiProducts;

public sealed class SyncCiceksepetiProductsHandler : IRequestHandler<SyncCiceksepetiProductsCommand, SyncResultDto>
{
    private readonly IIntegratorOrchestrator _orchestrator;

    public SyncCiceksepetiProductsHandler(IIntegratorOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task<SyncResultDto> Handle(SyncCiceksepetiProductsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _orchestrator.SyncPlatformAsync("CICEKSEPETI", cancellationToken).ConfigureAwait(false);
    }
}
