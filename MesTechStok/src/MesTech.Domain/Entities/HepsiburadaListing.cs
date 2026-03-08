using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Hepsiburada listing entity'si.
/// Hepsiburada'da urun listeleme durumu ve komisyon bilgisi.
/// </summary>
public class HepsiburadaListing : BaseEntity
{
    public string HepsiburadaSKU { get; set; } = string.Empty;
    public string MerchantSKU { get; set; } = string.Empty;
    public string ListingStatus { get; set; } = "Passive"; // Active, Passive, Banned
    public decimal CommissionRate { get; set; }
}
