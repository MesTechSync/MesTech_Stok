namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// FIFO maliyet hesaplama sonucu — urun bazinda COGS ozeti.
/// </summary>
public sealed class FifoCostResultDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int TotalPurchased { get; set; }
    public int TotalSold { get; set; }
    public int CurrentStock { get; set; }

    /// <summary>
    /// Toplam satilan mal maliyeti (FIFO yontemi).
    /// </summary>
    public decimal TotalCOGS { get; set; }

    /// <summary>
    /// FIFO sonrasi kalan envanterin agirlikli ortalama birim maliyeti.
    /// </summary>
    public decimal AverageCostPerUnit { get; set; }

    /// <summary>
    /// Satilmamis envanter katmanlari (FIFO sirasi ile).
    /// </summary>
    public IReadOnlyList<FifoLayerDto> RemainingLayers { get; set; } = Array.Empty<FifoLayerDto>();
}

/// <summary>
/// Tek bir FIFO maliyet katmani — alis partisi.
/// </summary>
public sealed class FifoLayerDto
{
    public DateTime PurchaseDate { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
