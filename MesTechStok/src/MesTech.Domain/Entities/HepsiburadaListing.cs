using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Hepsiburada listing bilgisi.
/// Listing durumlari: Active, Passive, Banned.
/// </summary>
public class HepsiburadaListing : BaseEntity
{
    public string HepsiburadaSKU { get; set; } = string.Empty;
    public string MerchantSKU { get; set; } = string.Empty;
    public string ListingStatus { get; set; } = "Passive";
    public decimal CommissionRate { get; set; }

    public bool IsActive => ListingStatus == "Active";
    public bool IsBanned => ListingStatus == "Banned";

    public override string ToString() => $"[HB-{HepsiburadaSKU}] {ListingStatus} ({CommissionRate:P1})";
}
