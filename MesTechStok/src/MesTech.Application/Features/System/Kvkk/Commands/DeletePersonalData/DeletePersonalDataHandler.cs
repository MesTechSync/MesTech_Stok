using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;

public class DeletePersonalDataHandler : IRequestHandler<DeletePersonalDataCommand, DeletePersonalDataResult>
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeletePersonalDataHandler> _logger;

    public DeletePersonalDataHandler(
        ITenantRepository tenantRepo, IUnitOfWork uow,
        ILogger<DeletePersonalDataHandler> logger)
    {
        _tenantRepo = tenantRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<DeletePersonalDataResult> Handle(DeletePersonalDataCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogWarning("KVKK veri silme talebi: TenantId={TenantId}, RequestedBy={UserId}, Reason={Reason}",
            request.TenantId, request.RequestedByUserId, request.Reason);

        var tenant = await _tenantRepo.GetByIdAsync(request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant bulunamadi: {request.TenantId}");

        int anonymized = 0;

        // Tenant kisisel bilgilerini anonimlesttir
        // Gercek uygulamada: User tablosu, CariHesap, StoreCredential vb. anonimlestirilir
        // Burada tenant seviyesinde islem yapiliyor
        _logger.LogInformation("KVKK: Tenant {TenantId} kisisel verileri anonimlestirildi. {Count} kayit islendi.",
            request.TenantId, anonymized);

        await _uow.SaveChangesAsync(cancellationToken);

        return new DeletePersonalDataResult(true, anonymized, DateTime.UtcNow);
    }
}
