namespace MesTech.Application.DTOs;

/// <summary>
/// Lot bilgisi DTO — FIFO görselleştirme ve maliyet hesaplama için.
/// </summary>
public sealed class InventoryLotDto
{
    public Guid Id { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost => Quantity * UnitCost;
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
}
