using System.Security.Cryptography;
using System.Text;
using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;

public sealed class DeletePersonalDataHandler : IRequestHandler<DeletePersonalDataCommand, DeletePersonalDataResult>
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IStoreRepository _storeRepo;
    private readonly IStoreCredentialRepository _credentialRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IUnitOfWork _uow;
    private readonly IKvkkAuditLogRepository _kvkkAuditRepo;
    private readonly ILogger<DeletePersonalDataHandler> _logger;

    public DeletePersonalDataHandler(
        ITenantRepository tenantRepo,
        IStoreRepository storeRepo,
        IStoreCredentialRepository credentialRepo,
        IOrderRepository orderRepo,
        IUnitOfWork uow,
        IKvkkAuditLogRepository kvkkAuditRepo,
        ILogger<DeletePersonalDataHandler> logger)
    {
        _tenantRepo = tenantRepo;
        _storeRepo = storeRepo;
        _credentialRepo = credentialRepo;
        _orderRepo = orderRepo;
        _uow = uow;
        _kvkkAuditRepo = kvkkAuditRepo;
        _logger = logger;
    }

    public async Task<DeletePersonalDataResult> Handle(DeletePersonalDataCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogWarning("KVKK veri silme talebi: TenantId={TenantId}, RequestedBy={UserId}, Reason={Reason}",
            request.TenantId, request.RequestedByUserId, request.Reason);

        var tenant = await _tenantRepo.GetByIdAsync(request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant bulunamadi: {request.TenantId}");

        var anonymized = 0;

        // 1. Tenant kisisel bilgilerini anonimlesttir
        tenant.Name = "ANONIM";
        tenant.TaxNumber = null;
        await _tenantRepo.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        anonymized++;

        _logger.LogInformation("KVKK: Tenant {TenantId} bilgileri anonimlestirildi.", request.TenantId);

        // 2. Tenant'a ait kullanicilari anonimlesttir
        // User.TenantId uzerinden, Tenant.Users navigation ile erisilebilir
        foreach (var user in tenant.Users)
        {
            user.FirstName = "ANONIM";
            user.LastName = null;
            user.Email = $"{ComputeHash(user.Id.ToString())}@anon.mestech.app";
            user.Phone = null;
            user.IsActive = false;
            anonymized++;
        }

        _logger.LogInformation("KVKK: {Count} kullanici anonimlestirildi, TenantId={TenantId}.",
            tenant.Users.Count, request.TenantId);

        // 3. Store credential'larini sil (API key, secret vb.)
        var stores = await _storeRepo.GetByTenantIdAsync(request.TenantId, cancellationToken);
        var credentialDeleteCount = 0;
        foreach (var store in stores)
        {
            var credentials = await _credentialRepo.GetByStoreIdAsync(store.Id, cancellationToken);
            foreach (var credential in credentials)
            {
                await _credentialRepo.DeleteAsync(credential, cancellationToken).ConfigureAwait(false);
                credentialDeleteCount++;
                anonymized++;
            }
        }

        _logger.LogInformation("KVKK: {Count} store credential silindi, TenantId={TenantId}.",
            credentialDeleteCount, request.TenantId);

        // 4. Siparis musteri bilgilerini anonimlesttir
        // Son 10 yilin siparislerini kapsayacak genis tarih araligi
        var orders = await _orderRepo.GetByDateRangeAsync(
            request.TenantId,
            DateTime.UtcNow.AddYears(-10),
            DateTime.UtcNow.AddDays(1),
            cancellationToken);

        foreach (var order in orders)
        {
            order.CustomerName = "ANONIM";
            order.CustomerEmail = null;
            await _orderRepo.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
            anonymized++;
        }

        _logger.LogInformation("KVKK: {Count} siparis musteri bilgisi anonimlestirildi, TenantId={TenantId}.",
            orders.Count, request.TenantId);

        // 5. KVKK denetim kaydı — yasal zorunluluk (defense in depth)
        var auditLog = KvkkAuditLog.Create(
            tenantId: request.TenantId,
            requestedByUserId: request.RequestedByUserId,
            operationType: KvkkOperationType.DataDeletion,
            reason: request.Reason,
            affectedRecordCount: anonymized,
            isSuccess: true,
            details: $"Tenant+{tenant.Users.Count} user+{credentialDeleteCount} credential+{orders.Count} order anonimlestirildi");
        await _kvkkAuditRepo.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            "KVKK TAMAMLANDI: TenantId={TenantId}, RequestedBy={UserId}, Reason={Reason}, " +
            "AnonymizedRecords={AnonymizedCount}, Timestamp={Timestamp}",
            request.TenantId, request.RequestedByUserId, request.Reason,
            anonymized, DateTime.UtcNow);

        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeletePersonalDataResult(true, anonymized, DateTime.UtcNow);
    }

    /// <summary>
    /// SHA256 hash uretir — e-posta anonimizasyonu icin deterministic pseudonym.
    /// </summary>
    private static string ComputeHash(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash)[..16];
    }
}
