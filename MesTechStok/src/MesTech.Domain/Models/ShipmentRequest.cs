using MesTech.Domain.ValueObjects;

namespace MesTech.Domain.Models;

/// <summary>
/// Kargo gonderim talebi — ICargoAdapter.CreateShipmentAsync icin.
/// </summary>
public class ShipmentRequest
{
    public Guid OrderId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public Address RecipientAddress { get; set; } = new();
    public Address SenderAddress { get; set; } = new();
    public decimal Weight { get; set; } // kg
    public int Desi { get; set; } // hacimsel agirlik
    public decimal? CodAmount { get; set; } // kapida odeme tutari (nullable)
    public int ParcelCount { get; set; } = 1;
    public string? Notes { get; set; }
}
