using System.Text.Json;
using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;

public sealed class ExportPersonalDataHandler : IRequestHandler<ExportPersonalDataQuery, PersonalDataExportDto>
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IStoreRepository _storeRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUserRepository _userRepo;
    private readonly IKvkkAuditLogRepository _kvkkAuditRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ExportPersonalDataHandler> _logger;

    public ExportPersonalDataHandler(
        ITenantRepository tenantRepo,
        IStoreRepository storeRepo,
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        IUserRepository userRepo,
        IKvkkAuditLogRepository kvkkAuditRepo,
        IUnitOfWork uow,
        ILogger<ExportPersonalDataHandler> logger)
    {
        _tenantRepo = tenantRepo;
        _storeRepo = storeRepo;
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _userRepo = userRepo;
        _kvkkAuditRepo = kvkkAuditRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<PersonalDataExportDto> Handle(ExportPersonalDataQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "KVKK veri disari aktarma talebi: TenantId={TenantId}, RequestedBy={UserId}",
            request.TenantId, request.RequestedByUserId);

        var tenant = await _tenantRepo.GetByIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tenant bulunamadi: {request.TenantId}");

        // Tenant'a ait tüm kişisel verileri topla
        var stores = await _storeRepo.GetByTenantIdAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        var orders = await _orderRepo.GetByDateRangeAsync(
            request.TenantId,
            DateTime.UtcNow.AddYears(-10),
            DateTime.UtcNow.AddDays(1),
            cancellationToken).ConfigureAwait(false);

        var productCount = await _productRepo.CountByTenantAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        var allUsers = await _userRepo.GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        // JSON export — KVKK madde 11/c uyumlu yapılandırılmış veri
        var exportData = new
        {
            meta = new
            {
                exportedAt = DateTime.UtcNow,
                tenantId = request.TenantId,
                requestedBy = request.RequestedByUserId,
                legalBasis = "KVKK madde 11/c — kisisel verilerin disari aktarilmasi hakki"
            },
            tenant = new
            {
                tenant.Id,
                tenant.Name,
                tenant.TaxNumber,
                tenant.CreatedAt
            },
            users = allUsers.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.FirstName,
                u.LastName,
                u.CreatedAt
            }),
            stores = stores.Select(s => new
            {
                s.Id,
                s.StoreName,
                PlatformType = s.PlatformType.ToString(),
                s.CreatedAt
            }),
            orders = orders.Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                o.CustomerEmail,
                o.TotalAmount,
                o.OrderDate,
                o.CreatedAt
            }),
            statistics = new
            {
                userCount = allUsers.Count,
                storeCount = stores.Count,
                orderCount = orders.Count,
                productCount
            }
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // KVKK denetim kaydı — yasal zorunluluk (defense in depth)
        var auditLog = KvkkAuditLog.Create(
            tenantId: request.TenantId,
            requestedByUserId: request.RequestedByUserId,
            operationType: KvkkOperationType.DataExport,
            reason: "KVKK madde 11/c — kisisel verilerin disari aktarilmasi",
            affectedRecordCount: allUsers.Count + stores.Count + orders.Count + productCount,
            isSuccess: true,
            details: $"Users={allUsers.Count}, Stores={stores.Count}, Orders={orders.Count}, Products={productCount}");
        await _kvkkAuditRepo.AddAsync(auditLog, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "KVKK export tamamlandi: TenantId={TenantId}, Users={Users}, Stores={Stores}, Orders={Orders}, Products={Products}",
            request.TenantId, allUsers.Count, stores.Count, orders.Count, productCount);

        return new PersonalDataExportDto
        {
            TenantId = request.TenantId,
            ExportedAt = DateTime.UtcNow,
            TenantName = tenant.Name,
            UserCount = allUsers.Count,
            StoreCount = stores.Count,
            OrderCount = orders.Count,
            ProductCount = productCount,
            DataJson = json
        };
    }
}
