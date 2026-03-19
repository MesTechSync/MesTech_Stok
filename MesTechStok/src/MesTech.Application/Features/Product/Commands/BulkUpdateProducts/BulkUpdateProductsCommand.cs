using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Product.Commands.BulkUpdateProducts;

/// <summary>
/// Seçili ürünlere toplu güncelleme komutu.
/// Fiyat ayarı, stok değişikliği, durum güncelleme vb.
/// </summary>
public record BulkUpdateProductsCommand(
    List<Guid> ProductIds,
    BulkUpdateAction Action,
    object? Value = null
) : IRequest<int>;
