using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Commands.SyncTrendyolProducts;

public sealed class SyncTrendyolProductsHandler : IRequestHandler<SyncTrendyolProductsCommand, SyncResultDto>
{
    private readonly IIntegratorOrchestrator _orchestrator;

    public SyncTrendyolProductsHandler(IIntegratorOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task<SyncResultDto> Handle(SyncTrendyolProductsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _orchestrator.SyncPlatformAsync("TRENDYOL", cancellationToken).ConfigureAwait(false);
    }
}
