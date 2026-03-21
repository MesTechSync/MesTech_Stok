using MediatR;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Erp.Commands.SyncOrderToErp;

/// <summary>
/// SyncOrderToErpCommand handler — siparisi ERP'ye senkronize eder ve sonucu loglar.
/// Dalga 11: ERP entegrasyonu icin eklendi.
/// </summary>
public class SyncOrderToErpHandler : IRequestHandler<SyncOrderToErpCommand, ErpSyncResult>
{
    private readonly IErpAdapterFactory _adapterFactory;
    private readonly IErpSyncLogRepository _syncLogRepository;
    private readonly IUnitOfWork _uow;

    public SyncOrderToErpHandler(
        IErpAdapterFactory adapterFactory,
        IErpSyncLogRepository syncLogRepository,
        IUnitOfWork uow)
    {
        _adapterFactory = adapterFactory;
        _syncLogRepository = syncLogRepository;
        _uow = uow;
    }

    public async Task<ErpSyncResult> Handle(
        SyncOrderToErpCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Sync log kaydi olustur
        var log = ErpSyncLog.Create(
            request.TenantId,
            request.Provider,
            entityType: "Order",
            entityId: request.OrderId);

        await _syncLogRepository.AddAsync(log, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        try
        {
            var adapter = _adapterFactory.GetAdapter(request.Provider);
            var result = await adapter.SyncOrderAsync(request.OrderId, cancellationToken);

            if (result.Success)
            {
                // ErpRef is guaranteed non-null when Success==true by the adapter contract
                log.MarkSuccess(result.ErpRef ?? string.Empty);
            }
            else
            {
                log.MarkFailure(result.ErrorMessage ?? "Bilinmeyen hata");
            }

            await _syncLogRepository.UpdateAsync(log, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return result;
        }
#pragma warning disable CA1031 // Intentional: ERP sync failure must be logged and returned, not propagated
        catch (Exception ex)
        {
            log.MarkFailure(ex.Message);
            await _syncLogRepository.UpdateAsync(log, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return ErpSyncResult.Fail(ex.Message);
        }
#pragma warning restore CA1031
    }
}
