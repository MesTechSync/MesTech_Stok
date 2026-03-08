using MesTech.Domain.ValueObjects;

namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Kargo gonderi olusturma istegi.
/// </summary>
public class ShipmentRequest
{
    public Guid OrderId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public Address RecipientAddress { get; set; } = new();
    public Address SenderAddress { get; set; } = new();
    public decimal Weight { get; set; }
    public int Desi { get; set; }
    public decimal? CodAmount { get; set; }
    public int ParcelCount { get; set; } = 1;
    public string? Notes { get; set; }
}
