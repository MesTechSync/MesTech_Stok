using MediatR;

namespace MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;

/// <summary>
/// Dropship sipariş kaydı oluşturur ve tedarikçiye sipariş referansıyla işaretler.
/// </summary>
public record PlaceDropshipOrderCommand(
    Guid TenantId,
    Guid OrderId,
    Guid SupplierId,
    Guid ProductId,
    string SupplierOrderRef
) : IRequest<Guid>;
