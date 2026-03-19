using MediatR;

namespace MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;

/// <summary>
/// Tedarikçi feed'inden fiyat senkronizasyonu komutu.
/// </summary>
public record SyncSupplierPricesCommand(
    Guid SupplierId
) : IRequest<PriceSyncResultDto>;

/// <summary>
/// Fiyat senkronizasyon sonuç DTO'su.
/// </summary>
public class PriceSyncResultDto
{
    public int Updated { get; init; }
    public int Unchanged { get; init; }
    public int Errors { get; init; }
    public List<PriceSyncErrorDto> ErrorDetails { get; init; } = [];
}

/// <summary>
/// Senkronizasyon sırasında oluşan hata detayı.
/// </summary>
public class PriceSyncErrorDto
{
    public string ExternalProductId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
