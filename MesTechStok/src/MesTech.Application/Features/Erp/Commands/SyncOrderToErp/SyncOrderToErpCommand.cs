using MediatR;
using MesTech.Application.DTOs.ERP;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Erp.Commands.SyncOrderToErp;

/// <summary>
/// Siparisi belirtilen ERP saglayicisina senkronize eder.
/// Dalga 11: ERP entegrasyonu icin eklendi.
/// </summary>
public record SyncOrderToErpCommand(
    Guid TenantId,
    Guid OrderId,
    ErpProvider Provider
) : IRequest<ErpSyncResult>;
