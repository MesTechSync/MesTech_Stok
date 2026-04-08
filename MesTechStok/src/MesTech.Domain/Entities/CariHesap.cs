using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Cari hesap (müşteri/tedarikçi) — OnMuhasebe modülü için.
/// </summary>
public sealed class CariHesap : BaseEntity, ITenantEntity
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

    public static CariHesap Create(Guid tenantId, string name, CariHesapType type, string? taxNumber = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var entity = new CariHesap
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Type = type,
            TaxNumber = taxNumber,
            CreatedAt = DateTime.UtcNow
        };

        entity.RaiseDomainEvent(new CariHesapCreatedEvent(
            entity.Id, tenantId, name, type, DateTime.UtcNow));

        return entity;
    }

    public void AddHareket(CariHareket hareket)
    {
        ArgumentNullException.ThrowIfNull(hareket);
        _hareketler.Add(hareket);

        RaiseDomainEvent(new CariHareketRecordedEvent(
            hareket.Id, Id, TenantId, hareket.Amount, hareket.Direction, DateTime.UtcNow));
    }

    /// <summary>
    /// Bakiye hesaplama: Borc hareketleri toplami - Alacak hareketleri toplami.
    /// Pozitif = borclu, Negatif = alacakli.
    /// </summary>
    public decimal GetBakiye()
    {
        var borc = _hareketler
            .Where(h => h.Direction == CariDirection.Borc)
            .Sum(h => h.Amount);
        var alacak = _hareketler
            .Where(h => h.Direction == CariDirection.Alacak)
            .Sum(h => h.Amount);
        return borc - alacak;
    }
}
