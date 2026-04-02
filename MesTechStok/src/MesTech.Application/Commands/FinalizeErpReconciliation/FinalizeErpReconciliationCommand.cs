using MediatR;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.FinalizeErpReconciliation;

public record FinalizeErpReconciliationCommand : IRequest
{
    public string ErpProvider { get; init; } = string.Empty;
    public int ReconciledCount { get; init; }
    public int MismatchCount { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class FinalizeErpReconciliationHandler : IRequestHandler<FinalizeErpReconciliationCommand>
{
    private readonly IErpSyncLogRepository _syncLogRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FinalizeErpReconciliationHandler> _logger;

    public FinalizeErpReconciliationHandler(
        IErpSyncLogRepository syncLogRepo,
        IUnitOfWork uow,
        ILogger<FinalizeErpReconciliationHandler> logger)
    {
        _syncLogRepo = syncLogRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task Handle(FinalizeErpReconciliationCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ErpProvider>(request.ErpProvider, ignoreCase: true, out var provider))
        {
            _logger.LogWarning("FinalizeErpReconciliation: Unknown ERP provider '{Provider}'", request.ErpProvider);
            return;
        }

        var log = ErpSyncLog.Create(
            request.TenantId,
            provider,
            "Reconciliation",
            Guid.NewGuid());

        if (request.MismatchCount == 0)
            log.MarkSuccess($"Reconciled:{request.ReconciledCount}");
        else
            log.MarkFailure($"{request.MismatchCount} mismatch detected");

        await _syncLogRepo.AddAsync(log, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "ERP reconciliation finalized: Provider={Provider} Reconciled={Reconciled} Mismatch={Mismatch} TenantId={TenantId}",
            request.ErpProvider, request.ReconciledCount, request.MismatchCount, request.TenantId);
    }
}
