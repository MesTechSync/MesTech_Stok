using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Cari hesap (müşteri/tedarikçi) — OnMuhasebe modülü için.
/// </summary>
public class CariHesap : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public CariHesapType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    // Navigation
    private readonly List<CariHareket> _hareketler = new();
    public IReadOnlyCollection<CariHareket> Hareketler => _hareketler.AsReadOnly();

    public void AddHareket(CariHareket hareket)
    {
        ArgumentNullException.ThrowIfNull(hareket);
        _hareketler.Add(hareket);
    }
}
