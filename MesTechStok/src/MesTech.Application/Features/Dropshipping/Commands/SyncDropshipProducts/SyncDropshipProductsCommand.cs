using MediatR;

namespace MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;

/// <summary>
/// Tedarikçiden ürün senkronizasyonu başlatır.
/// Placeholder: tedarikçi LastSyncAt güncellenir, mevcut ürünler fiyat/stok güncellenir.
/// </summary>
public record SyncDropshipProductsCommand(
    Guid TenantId,
    Guid SupplierId
) : IRequest<int>;
